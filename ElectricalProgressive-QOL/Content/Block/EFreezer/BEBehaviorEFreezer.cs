﻿using System.Linq;
using System.Text;
using ElectricalProgressive.Content.Block.ECharger;
using ElectricalProgressive.Interface;
using ElectricalProgressive.Utils;
using Vintagestory.API.Common;
using Vintagestory.API.Config;

namespace ElectricalProgressive.Content.Block.EFreezer;

public class BEBehaviorEFreezer : BlockEntityBehavior, IElectricConsumer
{
    public int powerSetting;
    public int maxConsumption;
    public BEBehaviorEFreezer(BlockEntity blockEntity) : base(blockEntity)
    {
        maxConsumption = MyMiniLib.GetAttributeInt(this.Block, "maxConsumption", 100);
    }

    public bool isBurned => this.Block.Variant["status"] == "burned";

    public void Consume_receive(float amount)
    {
        if (powerSetting != amount)
        {
            powerSetting = (int)amount;
        }
    }

    public float Consume_request()
    {
        return maxConsumption;
    }

    public override void GetBlockInfo(IPlayer forPlayer, StringBuilder stringBuilder)
    {
        base.GetBlockInfo(forPlayer, stringBuilder);

        //проверяем не сгорел ли прибор
        if (this.Api.World.BlockAccessor.GetBlockEntity(this.Blockentity.Pos) is BlockEntityEFreezer entity)
        {
            if (isBurned)
            {
                stringBuilder.AppendLine(Lang.Get("Burned"));
            }
            else
            {
                stringBuilder.AppendLine(StringHelper.Progressbar(powerSetting * 100.0f / maxConsumption));
                stringBuilder.AppendLine("└ " + Lang.Get("Consumption") + ": " + powerSetting + "/" + maxConsumption + " " + Lang.Get("W"));
            }
        }
        stringBuilder.AppendLine();
    }

    public float getPowerReceive()
    {
        return this.powerSetting;
    }

    public float getPowerRequest()
    {
        return maxConsumption;
    }

    public void Update()
    {
        //смотрим надо ли обновить модельку когда сгорает прибор
        if (this.Api.World.BlockAccessor.GetBlockEntity(this.Blockentity.Pos) is BlockEntityEFreezer entity && entity.AllEparams != null)
        {
            bool hasBurnout = entity.AllEparams.Any(e => e.burnout);

            if (hasBurnout)
            {
                ParticleManager.SpawnBlackSmoke(this.Api.World, Pos.ToVec3d().Add(0.5, 1.95, 0.5));
            }

            if (hasBurnout && entity.Block.Variant["status"] != "burned")
            {
                string type = "status";
                string variant = "burned";  

                this.Api.World.BlockAccessor.ExchangeBlock(Api.World.GetBlock(Block.CodeWithVariant(type, variant)).BlockId, Pos);
            }
        }

    }
}