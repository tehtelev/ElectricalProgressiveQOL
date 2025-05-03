using System;
using System.Text;
using ElectricalProgressive.Interface;
using ElectricalProgressive.Utils;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.GameContent;

namespace ElectricalProgressive.Content.Item.Tool;

class EChisel : ItemChisel,IEnergyStorageItem
{
    int consume;
    int maxcapacity;

    public override void OnLoaded(ICoreAPI api)
    {
        base.OnLoaded(api);

        consume = MyMiniLib.GetAttributeInt(this, "consume", 20);
        maxcapacity = MyMiniLib.GetAttributeInt(this, "maxcapacity", 20000);
        
    }


    /// <summary>
    /// Уменьшаем прочность
    /// </summary>
    /// <param name="world"></param>
    /// <param name="byEntity"></param>
    /// <param name="itemslot"></param>
    /// <param name="amount"></param>
    public override void DamageItem(IWorldAccessor world, Entity byEntity, ItemSlot itemslot, int amount = 1)
    {
        int energy = itemslot.Itemstack.Attributes.GetInt("electricalprogressive:energy");
        if (energy >= consume * amount)
        {
            energy -= consume * amount;
            itemslot.Itemstack.Item.SetDurability(itemslot.Itemstack, Math.Max(1, energy / consume));
            itemslot.Itemstack.Attributes.SetInt("electricalprogressive:energy", energy);
        }
        else
        {
            itemslot.Itemstack.Item.SetDurability(itemslot.Itemstack, 1);
        }
    }


    /// <summary>
    /// Нажатие левой кнопки
    /// </summary>
    /// <param name="slot"></param>
    /// <param name="byEntity"></param>
    /// <param name="blockSel"></param>
    /// <param name="entitySel"></param>
    /// <param name="handling"></param>
    public override void OnHeldAttackStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, ref EnumHandHandling handling)
    {
        int energy = slot.Itemstack.Attributes.GetInt("electricalprogressive:energy");
        if (energy >= consume)
        {
            energy -= consume;
            slot.Itemstack.Attributes.SetInt("durability", Math.Max(1, energy / consume));
            slot.Itemstack.Attributes.SetInt("electricalprogressive:energy", energy);
            base.OnHeldAttackStart(slot, byEntity, blockSel, entitySel, ref handling);
        }
        else
        {
            slot.Itemstack.Attributes.SetInt("durability", 1);
        }
        slot.MarkDirty();
    }

    /// <summary>
    /// Нажатие правой кнопки
    /// </summary>
    /// <param name="slot"></param>
    /// <param name="byEntity"></param>
    /// <param name="blockSel"></param>
    /// <param name="entitySel"></param>
    /// <param name="firstEvent"></param>
    /// <param name="handling"></param>
    public override void OnHeldInteractStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handling)
    {
        int energy = slot.Itemstack.Attributes.GetInt("electricalprogressive:energy");
        if (energy >= consume)
        {
            energy -= consume;
            slot.Itemstack.Attributes.SetInt("durability", Math.Max(1, energy / consume));
            slot.Itemstack.Attributes.SetInt("electricalprogressive:energy", energy);
            base.OnHeldInteractStart(slot, byEntity, blockSel, entitySel, firstEvent, ref handling);
        }
        else
        {
            slot.Itemstack.Attributes.SetInt("durability", 1);
        }
        slot.MarkDirty();
    }

    public override void GetHeldItemInfo(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo)
    {
        base.GetHeldItemInfo(inSlot, dsc, world, withDebugInfo);
        dsc.AppendLine(inSlot.Itemstack.Attributes.GetInt("electricalprogressive:energy") + "/" + maxcapacity + " " + Lang.Get("W"));
    }

    public int receiveEnergy(ItemStack itemstack, int maxReceive)
    {
        int received = Math.Min(maxcapacity - itemstack.Attributes.GetInt("electricalprogressive:energy"), maxReceive);
        itemstack.Attributes.SetInt("electricalprogressive:energy", itemstack.Attributes.GetInt("electricalprogressive:energy") + received);
        int durab = Math.Max(1, itemstack.Attributes.GetInt("electricalprogressive:energy") / consume);
        itemstack.Attributes.SetInt("durability", durab);
        return received;
    }
}