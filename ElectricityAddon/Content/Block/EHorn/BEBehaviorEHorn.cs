﻿using System.Text;
using ElectricityAddon.Interface;
using ElectricityAddon.Utils;
using Vintagestory.API.Common;
using Vintagestory.API.Config;

namespace ElectricityAddon.Content.Block.EHorn;

public class BEBehaviorEHorn : BlockEntityBehavior, IElectricConsumer
{
    private int maxTemp;
    public int powerSetting;
    private bool hasItems;
    public int maxConsumption;
    public BEBehaviorEHorn(BlockEntity blockEntity) : base(blockEntity)
    {
        maxConsumption = MyMiniLib.GetAttributeInt(this.Block, "maxConsumption", 100);
    }

    public void Consume(int amount)
    {
        BlockEntityEHorn? entity = null;
        if (Blockentity is BlockEntityEHorn temp)
        {
            entity = temp;
            hasItems = entity?.Contents?.StackSize > 0;
        }
        if (!hasItems)
        {
            amount = 0;
        }
        if (powerSetting != amount)
        {
            powerSetting = amount;
            maxTemp = amount * 1100 / maxConsumption;
            if (entity != null)
            {
                entity.IsBurning = amount > 0;
            }
        }
    }
    public override void GetBlockInfo(IPlayer forPlayer, StringBuilder stringBuilder)
    {
        base.GetBlockInfo(forPlayer, stringBuilder);
        stringBuilder.AppendLine(StringHelper.Progressbar(powerSetting));
        stringBuilder.AppendLine("├ " + Lang.Get("Consumption") + powerSetting + "/" + maxConsumption + " Eu");
        stringBuilder.AppendLine("└ " + Lang.Get("Temperature") + maxTemp + "° (max.)");
        stringBuilder.AppendLine();
    }

    public float Consume_request()
    {
        throw new System.NotImplementedException();
    }

    public void Consume_receive(float amount)
    {
        throw new System.NotImplementedException();
    }

    public void Update()
    {
        throw new System.NotImplementedException();
    }

    public float getPowerReceive()
    {
        throw new System.NotImplementedException();
    }

    public float getPowerRequest()
    {
        throw new System.NotImplementedException();
    }
}