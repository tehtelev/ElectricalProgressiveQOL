﻿

using ElectricalProgressive.Content.Block.EMotor;
using ElectricalProgressive.Utils;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace ElectricalProgressive.Content.Block.EHorn;

public class BlockEHorn : Vintagestory.API.Common.Block
{
    private WorldInteraction[]? interactions;

    public override void OnLoaded(ICoreAPI api)
    {
        base.OnLoaded(api);

        if (api is ICoreClientAPI clientApi)
        {
            this.interactions = ObjectCacheUtil.GetOrCreate(
                api,
                "forgeBlockInteractions",
                () =>
                {
                    var heatableStacklist = new List<ItemStack>();

                    foreach (
                        var stacks in
                        from obj in api.World.Collectibles
                        let firstCodePart = obj.FirstCodePart()
                        where firstCodePart == "ingot" || firstCodePart == "metalplate" || firstCodePart == "workitem"
                        select obj.GetHandBookStacks(clientApi)
                        into stacks
                        where stacks != null
                        select stacks
                    )
                    {
                        heatableStacklist.AddRange(stacks);
                    }

                    return new[]
                    {
                        new WorldInteraction
                        {
                            ActionLangCode = "blockhelp-forge-addworkitem",
                            HotKeyCode = "shift",
                            MouseButton = EnumMouseButton.Right,
                            Itemstacks = heatableStacklist.ToArray(),
                            GetMatchingStacks = (worldInteraction, blockSelection, _) =>
                            {
                                if (api.World.BlockAccessor.GetBlockEntity(blockSelection.Position) is
                                    BlockEntityEHorn { Contents: not null } bef)
                                {
                                    return worldInteraction.Itemstacks.Where(stack =>
                                            stack.Equals(api.World, bef.Contents,
                                                GlobalConstants.IgnoredStackAttributes))
                                        .ToArray();
                                }

                                return worldInteraction.Itemstacks;
                            }
                        },
                        new WorldInteraction
                        {
                            ActionLangCode = "blockhelp-forge-takeworkitem",
                            HotKeyCode = null,
                            MouseButton = EnumMouseButton.Right,
                            Itemstacks = heatableStacklist.ToArray(),
                            GetMatchingStacks = (_, blockSelection, _) =>
                            {
                                if (api.World.BlockAccessor.GetBlockEntity(blockSelection.Position) is
                                    BlockEntityEHorn { Contents: not null } bef)
                                {
                                    return new[]
                                    {
                                        bef.Contents
                                    };
                                }

                                return null;
                            }
                        }
                    };
                }
            );
        }
    }

    /// <summary>
    /// Кто-то или что-то коснулось блока и теперь получит урон
    /// </summary>
    /// <param name="world"></param>
    /// <param name="entity"></param>
    /// <param name="pos"></param>
    /// <param name="facing"></param>
    /// <param name="collideSpeed"></param>
    /// <param name="isImpact"></param>
    public override void OnEntityCollide(
        IWorldAccessor world,
        Entity entity,
        BlockPos pos,
        BlockFacing facing,
        Vec3d collideSpeed,
        bool isImpact
    )
    {
        // если это клиент, то не надо 
        if (world.Side == EnumAppSide.Client)
            return;

        // энтити не живой и не создание? выходим
        if (!entity.Alive || !entity.IsCreature)
            return;

        // получаем блокэнтити этого блока
        var blockentity = world.BlockAccessor.GetBlockEntity(pos) as BlockEntityEHorn;

        // если блокэнтити не найден, выходим
        if (blockentity == null)
            return;

        // передаем работу в наш обработчик урона
        ElectricalProgressive.damageManager.DamageEntity(world, entity, pos, facing, blockentity.AllEparams, this);

    }

    public override bool TryPlaceBlock(IWorldAccessor world, IPlayer byPlayer, ItemStack itemstack,
        BlockSelection blockSel, ref string failureCode)
    {
        return world.BlockAccessor
                   .GetBlock(blockSel.Position.AddCopy(BlockFacing.DOWN))
                   .SideSolid[BlockFacing.indexUP] &&
               base.TryPlaceBlock(world, byPlayer, itemstack, blockSel, ref failureCode);
    }

    public override void OnNeighbourBlockChange(IWorldAccessor world, BlockPos pos, BlockPos neibpos)
    {
        base.OnNeighbourBlockChange(world, pos, neibpos);

        if (
            !world.BlockAccessor
                .GetBlock(pos.AddCopy(BlockFacing.DOWN))
                .SideSolid[BlockFacing.indexUP]
        )
        {
            world.BlockAccessor.BreakBlock(pos, null);
        }
    }

    public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
    {
        if (world.BlockAccessor.GetBlockEntity(blockSel.Position) is BlockEntityEHorn entity)
        {
            return entity.OnPlayerInteract(world, byPlayer, blockSel);
        }

        return base.OnBlockInteractStart(world, byPlayer, blockSel);
    }

    public override ItemStack OnPickBlock(IWorldAccessor world, BlockPos pos)
    {
        AssetLocation blockCode = CodeWithVariants(new Dictionary<string, string>
        {
            { "state", (this.Variant["state"]=="enabled")? "disabled":(this.Variant["state"]=="disabled")? "disabled":"burned" },
            { "side", "south" }
        });

        Vintagestory.API.Common.Block block = world.BlockAccessor.GetBlock(blockCode);

        return new ItemStack(block);
    }

    public override ItemStack[] GetDrops(IWorldAccessor world, BlockPos pos, IPlayer byPlayer,
        float dropQuantityMultiplier = 1)
    {
        return new[] { OnPickBlock(world, pos) };
    }

    public override WorldInteraction[] GetPlacedBlockInteractionHelp(IWorldAccessor world, BlockSelection selection,
        IPlayer forPlayer)
    {
        return this.interactions.Append(base.GetPlacedBlockInteractionHelp(world, selection, forPlayer));
    }


    /// <summary>
    /// Проверка на возможность установки блока
    /// </summary>
    /// <param name="world"></param>
    /// <param name="byPlayer"></param>
    /// <param name="blockSelection"></param>
    /// <param name="byItemStack"></param>
    /// <returns></returns>
    public override bool DoPlaceBlock(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSelection, ItemStack byItemStack)
    {
        if (byItemStack.Block.Variant["state"] == "burned")
        {
            return false;
        }
        return base.DoPlaceBlock(world, byPlayer, blockSelection, byItemStack);
    }



    /// <summary>
    /// Получение информации о предмете в инвентаре
    /// </summary>
    /// <param name="inSlot"></param>
    /// <param name="dsc"></param>
    /// <param name="world"></param>
    /// <param name="withDebugInfo"></param>
    public override void GetHeldItemInfo(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo)
    {
        base.GetHeldItemInfo(inSlot, dsc, world, withDebugInfo);
        dsc.AppendLine(Lang.Get("Voltage") + ": " + MyMiniLib.GetAttributeInt(inSlot.Itemstack.Block, "voltage", 0) + " " + Lang.Get("V"));
        dsc.AppendLine(Lang.Get("Consumption") + ": " + MyMiniLib.GetAttributeFloat(inSlot.Itemstack.Block, "maxConsumption", 0) + " " + Lang.Get("W"));
        dsc.AppendLine(Lang.Get("WResistance") + ": " + ((MyMiniLib.GetAttributeBool(inSlot.Itemstack.Block, "isolatedEnvironment", false)) ? Lang.Get("Yes") : Lang.Get("No")));
    }
}