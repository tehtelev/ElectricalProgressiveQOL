﻿using System.Text;
using ElectricityAddon.Interface;
using ElectricityAddon.Utils;
using Vintagestory.API.Common;
using Vintagestory.API.Config;

namespace ElectricityAddon.Content.Block.EStove;

public class BEBehaviorEStove : BlockEntityBehavior, IElectricConsumer
{
    public int powerSetting;
    public bool working;
    private int stoveTemperature;
    public int maxConsumption = 0;
    public BEBehaviorEStove(BlockEntity blockEntity) : base(blockEntity)
    {
        maxConsumption = MyMiniLib.GetAttributeInt(this.Block, "maxConsumption", 100);
    }

    public void Consume(int amount)
    {
        BlockEntityEStove? entity = null;
        if (Blockentity is BlockEntityEStove temp)
        {
            entity = temp;
            working = entity.canHeatInput();
            stoveTemperature = (int)entity.stoveTemperature;
        }
        if (!working)
        {
            amount = 0;
        }
        if (powerSetting != amount)
        {
            powerSetting = amount;
        }
    }
    public override void GetBlockInfo(IPlayer forPlayer, StringBuilder stringBuilder)
    {
        base.GetBlockInfo(forPlayer, stringBuilder);
        stringBuilder.AppendLine(StringHelper.Progressbar(powerSetting * 100.0f / maxConsumption));
        stringBuilder.AppendLine("├ " + Lang.Get("Consumption") + powerSetting + "/" + maxConsumption + " Eu");  
        stringBuilder.AppendLine("└ " + Lang.Get("Temperature") + stoveTemperature + "°");
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