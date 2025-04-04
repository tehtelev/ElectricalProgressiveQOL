﻿using ElectricalProgressive.Utils;
using System;
using System.Text;
using ElectricalProgressive.Interface;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using System.Linq;

namespace ElectricalProgressive.Content.Block.EFonar
{
    public class BEBehaviorEFonar : BlockEntityBehavior, IElectricConsumer
    {
        public BEBehaviorEFonar(BlockEntity blockEntity) : base(blockEntity)
        {
            maxConsumption = MyMiniLib.GetAttributeInt(Block, "maxConsumption", 4);
        }



        public int maxConsumption;              //максимальное потребление
        public bool isBurned => Block.Variant["state"] == "burned";
        public int LightLevel { get; private set; }


        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            base.ToTreeAttributes(tree);
            tree.SetInt("electricalprogressive:LightLevel", LightLevel);

        }

        public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldAccessForResolve)
        {
            base.FromTreeAttributes(tree, worldAccessForResolve);
            LightLevel = tree.GetInt("electricalprogressive:LightLevel");

        }


        public float Consume_request()
        {
            return maxConsumption;
        }


        public void Consume_receive(float amount)
        {
            if (Api is { } api)
            {
                int amountInt = (int)Math.Round(amount, MidpointRounding.AwayFromZero);

                if (amountInt != LightLevel && Block.Variant["state"] != "burned")
                {

                    if (amountInt >= 1 && Block.Variant["state"] == "disabled")                               //включаем если питание больше 1
                    {
                        api.World.BlockAccessor.ExchangeBlock(Api.World.GetBlock(Block.CodeWithVariant("state", "enabled")).BlockId, Pos);

                    }
                    else if (amountInt < 1 && Block.Variant["state"] == "enabled")                            //гасим если питание меньше 1
                    {
                        api.World.BlockAccessor.ExchangeBlock(Api.World.GetBlock(Block.CodeWithVariant("state", "disabled")).BlockId, Pos);

                    }
                    else
                    {
                        api.World.BlockAccessor.ExchangeBlock(Api.World.GetBlock(Blockentity.Block.BlockId).BlockId, Pos);
                    }

                    // в любом случае обновляем значение
                    LightLevel = amountInt;
                    Blockentity.MarkDirty(true);


                }


            }
        }


        public float getPowerReceive()
        {
            return LightLevel;
        }

        public float getPowerRequest()
        {
            return maxConsumption;
        }

        public void Update()
        {
            //смотрим надо ли обновить модельку когда сгорает прибор
            if (Api.World.BlockAccessor.GetBlockEntity(Blockentity.Pos) is BlockEntityEFonar entity && entity.AllEparams != null)
            {

                bool hasBurnout = entity.AllEparams.Any(e => e.burnout);
                if (hasBurnout && entity.Block.Variant["state"] != "burned")
                {
                    Api.World.BlockAccessor.ExchangeBlock(Api.World.GetBlock(Block.CodeWithVariant("state", "burned")).BlockId, Pos);
                }

            }



        }


        public override void GetBlockInfo(IPlayer forPlayer, StringBuilder stringBuilder)
        {
            base.GetBlockInfo(forPlayer, stringBuilder);

            //проверяем не сгорел ли прибор
            if (Api.World.BlockAccessor.GetBlockEntity(Blockentity.Pos) is BlockEntityEFonar entity)
            {
                if (isBurned)
                {
                    stringBuilder.AppendLine(Lang.Get("Burned"));
                }
                else
                {
                    stringBuilder.AppendLine(StringHelper.Progressbar(LightLevel * 100.0f / maxConsumption));
                    stringBuilder.AppendLine("└ " + Lang.Get("Consumption") + ": " + LightLevel + "/" + maxConsumption + " " + Lang.Get("W"));
                }

            }
            stringBuilder.AppendLine();
        }


    }
}
