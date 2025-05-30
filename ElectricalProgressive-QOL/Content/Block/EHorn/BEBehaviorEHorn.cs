﻿using System.Linq;
using System.Text;
using ElectricalProgressive.Content.Block.EAccumulator;
using ElectricalProgressive.Interface;
using ElectricalProgressive.Utils;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;

namespace ElectricalProgressive.Content.Block.EHorn;

public class BEBehaviorEHorn : BlockEntityBehavior, IElectricConsumer
{
    private float maxTemp;
    private float maxTargetTemp;

    private float powerRequest = maxConsumption;         // Нужно энергии (сохраняется)
    private float powerReceive = 0;             // Дали энергии  (сохраняется)


    public bool isBurned => this.Block.Variant["state"] == "burned";


    public bool hasItems
    {
        get
        {
            bool w = false;
            BlockEntityEHorn? entity = null;
            if (Blockentity is BlockEntityEHorn temp)
            {
                entity = temp;
                w = entity?.Contents?.StackSize > 0;
            }
            return w;
        }
    }




    public static int maxConsumption;

    public BEBehaviorEHorn(BlockEntity blockEntity) : base(blockEntity)
    {
        maxConsumption = MyMiniLib.GetAttributeInt(this.Block, "maxConsumption", 100);
        maxTargetTemp = MyMiniLib.GetAttributeFloat(this.Block, "maxTargetTemp", 1100.0F);
    }

    public override void GetBlockInfo(IPlayer forPlayer, StringBuilder stringBuilder)
    {
        base.GetBlockInfo(forPlayer, stringBuilder);

        //проверяем не сгорел ли прибор
        if (this.Api.World.BlockAccessor.GetBlockEntity(this.Blockentity.Pos) is BlockEntityEHorn entity)
        {
            if (isBurned)
            {
                stringBuilder.AppendLine(Lang.Get("Burned"));
                entity.IsBurning = false;
            }
            else
            {
                stringBuilder.AppendLine(StringHelper.Progressbar(powerReceive / maxConsumption * 100));
                stringBuilder.AppendLine("└ " + Lang.Get("Consumption") + ": " + powerReceive + "/" + maxConsumption + " " + Lang.Get("W"));
                stringBuilder.AppendLine("└ " + Lang.Get("Temperature") + ": " + maxTemp + "° ("+ Lang.Get("max") + ")");
            }

        }

        stringBuilder.AppendLine();
    }


    public float Consume_request()
    {
        if (hasItems)
            return this.powerRequest;
        else
            return 0;
    }


    public void Consume_receive(float amount)
    {

        if (!hasItems)
        {
            amount = 0;
        }

        if (this.powerReceive != amount)
        {
            this.powerReceive = amount;
            maxTemp = amount * maxTargetTemp / maxConsumption;

        }
    }

    public void Update()
    {
        //смотрим надо ли обновить модельку когда сгорает прибор
        if (this.Api.World.BlockAccessor.GetBlockEntity(this.Blockentity.Pos) is BlockEntityEHorn entity && entity.AllEparams != null)
        {
            bool hasBurnout = entity.AllEparams.Any(e => e.burnout);

            if (hasBurnout)
            {
                ParticleManager.SpawnBlackSmoke(this.Api.World, Pos.ToVec3d().Add(0.5, 0.5, 0.5));
            }

            if (hasBurnout && entity.Block.Variant["state"] != "burned")
            {
                string side = entity.Block.Variant["side"];

                string[] types = new string[2] { "state", "side" };   //типы горна
                string[] variants = new string[2] { "burned", side };  //нужный вариант 

                this.Api.World.BlockAccessor.ExchangeBlock(Api.World.GetBlock(Block.CodeWithVariants(types, variants)).BlockId, Pos);
            }
        }


    }

    public float getPowerReceive()
    {
        return this.powerReceive;
    }

    public float getPowerRequest()
    {
        if (hasItems)
            return this.powerRequest;
        else
            return 0;
        
    }


    public override void ToTreeAttributes(ITreeAttribute tree)
    {
        base.ToTreeAttributes(tree);
        tree.SetFloat("electricalprogressive:powerRequest", powerRequest);
        tree.SetFloat("electricalprogressive:powerRecieve", powerReceive);
    }

    public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldAccessForResolve)
    {
        base.FromTreeAttributes(tree, worldAccessForResolve);
        powerRequest = tree.GetFloat("electricalprogressive:powerRequest");
        powerReceive = tree.GetFloat("electricalprogressive:powerReceive");
    }

}