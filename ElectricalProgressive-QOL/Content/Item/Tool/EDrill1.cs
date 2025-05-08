using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ElectricalProgressive.Interface;
using ElectricalProgressive.Utils;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace ElectricalProgressive.Content.Item.Tool;

public class EDrill1 : Vintagestory.API.Common.Item, IEnergyStorageItem
{
    public virtual int MultiBreakQuantity => 8;

    public virtual bool CanMultiBreak(Vintagestory.API.Common.Block block)
    {
        if (block.BlockMaterial == EnumBlockMaterial.Soil || block.BlockMaterial == EnumBlockMaterial.Gravel ||
            block.BlockMaterial == EnumBlockMaterial.Ore || block.BlockMaterial == EnumBlockMaterial.Stone)
        {
            return true;
        }
        return false;
    }

    public SkillItem[] toolModes;
    int consume;
    int maxcapacity;


    public override void OnLoaded(ICoreAPI api)
    {
        base.OnLoaded(api);

        consume = MyMiniLib.GetAttributeInt(this, "consume", 20);
        maxcapacity = MyMiniLib.GetAttributeInt(this, "maxcapacity", 20000);
        Durability = maxcapacity / consume;
        ICoreClientAPI capi = (api as ICoreClientAPI)!;
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

    public override void SetToolMode(
        ItemSlot slot,
        IPlayer byPlayer,
        BlockSelection blockSel,
        int toolMode)
    {
        ItemSlot mouseItemSlot = byPlayer.InventoryManager.MouseItemSlot;
        if (!mouseItemSlot.Empty && mouseItemSlot.Itemstack.Block != null)
        {
            api.Event.PushEvent("keepopentoolmodedlg");
        }
        else
            slot.Itemstack.Attributes.SetInt(nameof(toolMode), toolMode);
    }

    public override void DamageItem(IWorldAccessor world, Entity byEntity, ItemSlot itemslot, int amount = 1)
    {
        int energy = itemslot.Itemstack.Attributes.GetInt("electricalprogressive:energy");
        if (energy >= consume * amount)
        {
            energy -= consume * amount;
            itemslot.Itemstack.Attributes.SetInt("durability", Math.Max(1, energy / consume));
            itemslot.Itemstack.Attributes.SetInt("electricalprogressive:energy", energy);
        }
        else
        {
            itemslot.Itemstack.Attributes.SetInt("durability", 1);
        }
        itemslot.MarkDirty();
    }

    public override void GetHeldItemInfo(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo)
    {
        base.GetHeldItemInfo(inSlot, dsc, world, withDebugInfo);
        dsc.AppendLine(inSlot.Itemstack.Attributes.GetInt("electricalprogressive:energy") + "/" + maxcapacity + " " + Lang.Get("J"));
    }

    public int receiveEnergy(ItemStack itemstack, int maxReceive)
    {
        int received = Math.Min(maxcapacity - itemstack.Attributes.GetInt("electricalprogressive:energy"), maxReceive);
        itemstack.Attributes.SetInt("electricalprogressive:energy", itemstack.Attributes.GetInt("electricalprogressive:energy") + received);
        int durab = Math.Max(1, itemstack.Attributes.GetInt("electricalprogressive:energy") / consume);
        itemstack.Attributes.SetInt("durability", durab);
        return received;
    }

    public override float OnBlockBreaking(
      IPlayer player,
      BlockSelection blockSel,
      ItemSlot itemslot,
      float remainingResistance,
      float dt,
      int counter)
    {
        float num = base.OnBlockBreaking(player, blockSel, itemslot, remainingResistance, dt, counter);
        int remainingDurability = itemslot.Itemstack.Collectible.GetRemainingDurability(itemslot.Itemstack);
        DamageNearbyBlocks(player, blockSel, remainingResistance - num, remainingDurability, itemslot);
        return num;
    }

    private void DamageNearbyBlocks(
      IPlayer player,
      BlockSelection blockSel,
      float damage,
      int leftDurability, ItemSlot itemslot)
    {
        if (!CanMultiBreak(player.Entity.World.BlockAccessor.GetBlock(blockSel.Position)))
            return;
        Vec3d hitPos = blockSel.Position.ToVec3d().Add(blockSel.HitPosition);
        IEnumerable<BlockPos> blockPoses = GetNearblyMultibreakables(player.Entity.World, blockSel.Position, hitPos).OrderBy(x => x.Value).Select(x => x.Key);
        int num = Math.Min(MultiBreakQuantity, leftDurability);
        foreach (BlockPos pos in blockPoses)
        {
            if (num == 0)
                break;
            BlockFacing opposite = BlockFacing.FromNormal(player.Entity.ServerPos.GetViewVector()).Opposite;
            if (player.Entity.World.Claims.TryAccess(player, pos, EnumBlockAccessFlags.BuildOrBreak) && itemslot.Itemstack.Collectible.GetRemainingDurability(itemslot.Itemstack) > 1 && GetToolMode(itemslot, player, blockSel) == 1)
            {
                player.Entity.World.BlockAccessor.DamageBlock(pos, opposite, damage);
                --num;
            }
        }
    }

    public override bool OnBlockBrokenWith(
      IWorldAccessor world,
      Entity byEntity,
      ItemSlot itemslot,
      BlockSelection blockSel,
      float dropQuantityMultiplier = 1f)
    {
        Vintagestory.API.Common.Block block = world.BlockAccessor.GetBlock(blockSel.Position);
        if (itemslot.Itemstack.Collectible.GetRemainingDurability(itemslot.Itemstack) <= 1)
            return false;
        if (!(byEntity is EntityPlayer) || itemslot.Itemstack == null)
            return true;
        IPlayer player = world.PlayerByUid((byEntity as EntityPlayer)!.PlayerUID);
        breakMultiBlock(blockSel.Position, player);
        if (!CanMultiBreak(block))
            return true;
        Vec3d hitPos = blockSel.Position.ToVec3d().Add(blockSel.HitPosition);
        IOrderedEnumerable<KeyValuePair<BlockPos, float>> orderedEnumerable = GetNearblyMultibreakables(world, blockSel.Position, hitPos).OrderBy(x => x.Value);
        itemslot.Itemstack.Collectible.GetRemainingDurability(itemslot.Itemstack);
        int num = 0;
        foreach (KeyValuePair<BlockPos, float> keyValuePair in (IEnumerable<KeyValuePair<BlockPos, float>>)orderedEnumerable)
        {
            if (player.Entity.World.Claims.TryAccess(player, keyValuePair.Key, EnumBlockAccessFlags.BuildOrBreak))
            {
                DamageItem(world, byEntity, itemslot);
                if (GetToolMode(itemslot, player, blockSel) == 0)
                    break;
                breakMultiBlock(keyValuePair.Key, player);
                ++num;
                if (num < MultiBreakQuantity)
                {
                    if (itemslot.Itemstack == null)
                        break;
                }
                else
                    break;
            }
        }
        return true;
    }

    protected virtual void breakMultiBlock(BlockPos pos, IPlayer plr)
    {
        api.World.BlockAccessor.BreakBlock(pos, plr);
        api.World.BlockAccessor.MarkBlockDirty(pos);
    }

    private OrderedDictionary<BlockPos, float> GetNearblyMultibreakables(
      IWorldAccessor world,
      BlockPos pos,
      Vec3d hitPos)
    {
        OrderedDictionary<BlockPos, float> nearblyMultibreakables = new OrderedDictionary<BlockPos, float>();
        for (int dx = -1; dx <= 1; ++dx)
        {
            for (int dy = -1; dy <= 1; ++dy)
            {
                for (int dz = -1; dz <= 1; ++dz)
                {
                    if (dx != 0 || dy != 0 || dz != 0)
                    {
                        BlockPos blockPos = pos.AddCopy(dx, dy, dz);
                        if (CanMultiBreak(world.BlockAccessor.GetBlock(blockPos)))
                            nearblyMultibreakables.Add(blockPos, hitPos.DistanceTo(blockPos.X + 0.7, blockPos.Y + 0.7, blockPos.Z + 0.7));
                    }
                }
            }
        }
        return nearblyMultibreakables;
    }
}