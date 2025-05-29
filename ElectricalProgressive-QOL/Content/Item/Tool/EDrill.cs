﻿using System;
using System.Text;
using ElectricalProgressive.Interface;
using ElectricalProgressive.Utils;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

namespace ElectricalProgressive.Content.Item.Tool;

class EDrill : Vintagestory.API.Common.Item,IEnergyStorageItem
{
    public SkillItem[] toolModes;
    int consume;



    public override void OnLoaded(ICoreAPI api)
    {
        base.OnLoaded(api);

        consume = MyMiniLib.GetAttributeInt(this, "consume", 20);


        //режимы дрели
        ICoreClientAPI capi = api as ICoreClientAPI;
        if (capi == null)
            return;



        toolModes = ObjectCacheUtil.GetOrCreate(api, "drillToolModes", () => new SkillItem[2]
        {
            new SkillItem
            {
                Code = new AssetLocation("1size"),
                Name = Lang.Get("drill1")
            }.WithIcon(capi, IconStorage.DrawTool1x1),
            new SkillItem
            {
                Code = new AssetLocation("3size"),
                Name = Lang.Get("drill2")
            }.WithIcon(capi, IconStorage.DrawTool1x3)
        });
    }


    public override SkillItem[] GetToolModes(ItemSlot slot, IClientPlayer forPlayer, BlockSelection blockSel)
    {
        return toolModes;
    }

    public override int GetToolMode(ItemSlot slot, IPlayer byPlayer, BlockSelection blockSel)
    {
        return slot.Itemstack.Attributes.GetInt("toolMode");
    }
    public override void OnUnloaded(ICoreAPI api)
    {
        for (int index = 0; toolModes != null && index < toolModes.Length; ++index)
            toolModes[index]?.Dispose();
    }

    /// <summary>
    /// Задаем режимы
    /// </summary>
    /// <param name="slot"></param>
    /// <param name="byPlayer"></param>
    /// <param name="blockSel"></param>
    /// <param name="toolMode"></param>
    public override void SetToolMode(
        ItemSlot slot,
        IPlayer byPlayer,
        BlockSelection blockSel,
        int toolMode)
    {
        ItemSlot mouseItemSlot = byPlayer.InventoryManager.MouseItemSlot;
        if (!mouseItemSlot.Empty && mouseItemSlot.Itemstack.Block != null )
        {
            api.Event.PushEvent("keepopentoolmodedlg");
        }
        else
            slot.Itemstack.Attributes.SetInt(nameof(toolMode), toolMode);
    }

    /// <summary>
    /// Уменьшение прочности
    /// </summary>
    /// <param name="world"></param>
    /// <param name="byEntity"></param>
    /// <param name="itemslot"></param>
    /// <param name="amount"></param>
    public override void DamageItem(IWorldAccessor world, Entity byEntity, ItemSlot itemslot, int amount)
    {
        int durability = itemslot.Itemstack.Attributes.GetInt("durability");
        if (durability > amount)
        {
            durability -= amount;
            itemslot.Itemstack.Attributes.SetInt("durability", durability);
        }
        else
        {
            durability = 1;
            itemslot.Itemstack.Attributes.SetInt("durability", durability);
        }

        itemslot.MarkDirty();
    }
    
    /// <summary>
    /// Информация о предмете
    /// </summary>
    /// <param name="inSlot"></param>
    /// <param name="dsc"></param>
    /// <param name="world"></param>
    /// <param name="withDebugInfo"></param>
    public override void GetHeldItemInfo(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo)
    {
        base.GetHeldItemInfo(inSlot, dsc, world, withDebugInfo);

        int energy = inSlot.Itemstack.Attributes.GetInt("durability") * consume; //текущая энергия
        int maxEnergy = inSlot.Itemstack.Collectible.GetMaxDurability(inSlot.Itemstack) * consume;       //максимальная энергия
        dsc.AppendLine(energy + "/" + maxEnergy + " " + Lang.Get("J"));
    }



    /// <summary>
    /// Зарядка
    /// </summary>
    /// <param name="itemstack"></param>
    /// <param name="maxReceive"></param>
    /// <returns></returns>
    public int receiveEnergy(ItemStack itemstack, int maxReceive)
    {
        int energy = itemstack.Attributes.GetInt("durability") * consume; //текущая энергия
        int maxEnergy = itemstack.Collectible.GetMaxDurability(itemstack) * consume;       //максимальная энергия

        int received = Math.Min(maxEnergy - energy, maxReceive);

        energy += received;

        int durab = Math.Max(1, energy / consume);
        itemstack.Attributes.SetInt("durability", durab);
        return received;
    }



    public override void OnHeldAttackStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, ref EnumHandHandling handling)
    {


        base.OnHeldAttackStart(slot, byEntity, blockSel, entitySel, ref handling);
    }


    public override void OnHeldAttackStop(float secondsPassed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSelection, EntitySelection entitySel)
    {
        base.OnHeldAttackStop(secondsPassed, slot, byEntity, blockSelection, entitySel);


    }




    /// <summary>
    /// Ломание боков дрелью
    /// </summary>
    /// <param name="world"></param>
    /// <param name="byEntity"></param>
    /// <param name="slot"></param>
    /// <param name="blockSel"></param>
    /// <param name="dropQuantityMultiplier"></param>
    /// <returns></returns>
    public override bool OnBlockBrokenWith(IWorldAccessor world, Entity byEntity, ItemSlot slot, BlockSelection blockSel, float dropQuantityMultiplier = 1)
    {
        int durability = slot.Itemstack.Attributes.GetInt("durability");
        if (durability > 1)
        {
            DamageItem(world,byEntity,slot,1);
            if (base.OnBlockBrokenWith(world, byEntity, slot, blockSel, dropQuantityMultiplier))
            {
                if (byEntity is EntityPlayer)
                {
                    var player = world.PlayerByUid((byEntity as EntityPlayer).PlayerUID);
                    {
                        var selection = new Selection(blockSel);

                        if (GetToolMode(slot,player,blockSel) == 1) //второй режим 1х3
                        {
                            switch (blockSel.Face.Axis)
                            {
                                case EnumAxis.X: //x грань
                                    
                                    if (selection.Direction == BlockFacing.DOWN || selection.Direction == BlockFacing.UP) // смотрим в сторону Y
                                    {
                                        destroyBlocks(world, blockSel.Position.AddCopy(0, -1, 0),
                                        blockSel.Position.AddCopy(0, 1, 0), player, blockSel, slot);
                                    }
                                    else if (selection.Direction == BlockFacing.SOUTH || selection.Direction == BlockFacing.NORTH) // смотрим в сторону z
                                    {
                                        destroyBlocks(world, blockSel.Position.AddCopy(0, 0, -1),
                                        blockSel.Position.AddCopy(0, 0, 1), player, blockSel, slot);
                                    }
                                    break;
                                case EnumAxis.Y: //y грань
                                    
                                    if (selection.Direction == BlockFacing.EAST || selection.Direction == BlockFacing.WEST) // смотрим в сторону x
                                    {
                                        destroyBlocks(world, blockSel.Position.AddCopy(-1, 0, 0),
                                        blockSel.Position.AddCopy(1, 0, 0), player, blockSel, slot);
                                    }
                                    else if (selection.Direction == BlockFacing.SOUTH || selection.Direction == BlockFacing.NORTH) // смотрим в сторону z
                                    {
                                        destroyBlocks(world, blockSel.Position.AddCopy(0, 0, -1),
                                        blockSel.Position.AddCopy(0, 0, 1), player, blockSel, slot);
                                    }
                                    break;
                                case EnumAxis.Z: //z грань
                                    if (selection.Direction == BlockFacing.DOWN || selection.Direction == BlockFacing.UP) // смотрим в сторону Y
                                    {
                                        destroyBlocks(world, blockSel.Position.AddCopy(0, -1, 0),
                                        blockSel.Position.AddCopy(0, 1, 0), player, blockSel, slot);
                                    }
                                    else if (selection.Direction == BlockFacing.EAST || selection.Direction == BlockFacing.WEST) // смотрим в сторону x
                                    {
                                        destroyBlocks(world, blockSel.Position.AddCopy(-1, 0, 0),
                                        blockSel.Position.AddCopy(1, 0, 0), player, blockSel, slot);
                                    }
                                    break;
                            }
                        }
                        else
                        {
                            switch (blockSel.Face.Axis) //первый режим
                            {
                                case EnumAxis.X:
                                    destroyBlocks(world, blockSel.Position,
                                        blockSel.Position, player, blockSel, slot);
                                    break;
                                case EnumAxis.Y:
                                    destroyBlocks(world, blockSel.Position,
                                        blockSel.Position, player, blockSel, slot);
                                    break;
                                case EnumAxis.Z:
                                    destroyBlocks(world, blockSel.Position,
                                        blockSel.Position, player, blockSel, slot);
                                    break;
                            } 
                        }
                    }

                }
                return true;
            }
            return false;
        }
        return false;
    }

    /// <summary>
    /// Ломает блоки в заданном диапазоне
    /// </summary>
    /// <param name="world"></param>
    /// <param name="min"></param>
    /// <param name="max"></param>
    /// <param name="player"></param>
    /// <param name="block"></param>
    /// <param name="slot"></param>
    //credit to stitch37 for this code
    public void destroyBlocks(IWorldAccessor world, BlockPos min, BlockPos max, IPlayer player,BlockSelection block, ItemSlot slot)
    {
        int durability = slot.Itemstack.Attributes.GetInt("durability"); 
        var wBA = world.BlockAccessor;  //тяжелая штука, нужно разочек обьявить
        var centerBlock = wBA.GetBlock(block.Position);
        var itemStack = new ItemStack(this);
        Vintagestory.API.Common.Block tempBlock;
        var miningTimeMainBlock = GetMiningSpeed(itemStack, block,centerBlock, player);
        float miningTime;
        var tempPos = new BlockPos();

        for (int x = min.X; x <= max.X; x++)
        {
            for (int y = min.Y; y <= max.Y; y++)
            {
                for (int z = min.Z; z <= max.Z; z++)
                {
                    tempPos.Set(x, y, z);
                    tempBlock = wBA.GetBlock(tempPos);
                    if (player.WorldData.CurrentGameMode == EnumGameMode.Creative)
                        wBA.SetBlock(0, tempPos);
                    else
                    {
                        if (durability > 1)
                        {
                            miningTime = tempBlock.GetMiningSpeed(itemStack, block,tempBlock, player);
                            if (ToolTier >= tempBlock.RequiredMiningTier
                                && miningTimeMainBlock * 1.5f >= miningTime
                                && MiningSpeed.ContainsKey(tempBlock.BlockMaterial))

                            {
                                wBA.BreakBlock(tempPos, player);
                            }
                        }
                    }
                }
            }
        }
    }    
}