﻿using System;
using System.Text;
using ElectricityAddon.Interface;
using ElectricityAddon.Utils;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;


namespace ElectricityAddon.Content.Block.EAccumulator;

public class BEBehaviorEAccumulator : BlockEntityBehavior, IElectricAccumulator {

    public float capacity;
    public BEBehaviorEAccumulator(BlockEntity blockEntity) : base(blockEntity) {
    }


    public BlockPos Pos => this.Blockentity.Pos;

    public float maxCurrent => 200.0F;   //ограничение по току!!!!!!!

    public float GetMaxCapacity()
    {
        return MyMiniLib.GetAttributeInt(this.Block, "maxcapacity",16000);
    }

    public float GetCapacity() {
        return capacity;
    }



    public void Store(float amount)
    {
        var buf = Math.Min(Math.Min(amount, maxCurrent), GetMaxCapacity()-capacity);

        capacity += buf;  //не позволяем одним пакетом сохранить больше максимального тока. В теории такого превышения и не должно случиться
    }

    public float Release(float amount)
    {
        var buf= Math.Min(capacity, Math.Min(amount, maxCurrent));
        capacity -= buf;
        return buf;                                                 //выдаем пакет c учетом тока и запасов
    }


    public float canStore()
    {
        return Math.Min( maxCurrent, GetMaxCapacity() - capacity);
    }

    public float canRelease()
    {
        return Math.Min(capacity, maxCurrent);
    }


    public override void ToTreeAttributes(ITreeAttribute tree) {
        base.ToTreeAttributes(tree);
        tree.SetFloat("electricity:energy", capacity);
    }

    public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldAccessForResolve) {
        base.FromTreeAttributes(tree, worldAccessForResolve);
        capacity = tree.GetFloat("electricity:energy");
    }


    public override void GetBlockInfo(IPlayer forPlayer, StringBuilder stringBuilder) {
        base.GetBlockInfo(forPlayer, stringBuilder);
        stringBuilder.AppendLine(StringHelper.Progressbar(GetCapacity() * 100.0f / GetMaxCapacity()));
        stringBuilder.AppendLine("└ " + Lang.Get("Storage") + GetCapacity() + "/" + GetMaxCapacity() + " Eu");
        stringBuilder.AppendLine();
    }


}