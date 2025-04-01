using ElectricalProgressive.Utils;
using System;
using System.Text;
using ElectricalProgressive.Interface;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using ElectricalProgressive.Content.Block.ETransformator;
using System.Linq;
using ElectricalProgressive.Content.Block.EHorn;
using ElectricalProgressive.Content.Block.EHeater;
using Vintagestory.API.Client;

namespace ElectricalProgressive.Content.Block.ELamp
{
    public class BEBehaviorELamp : BlockEntityBehavior, IElectricConsumer
    {
        public BEBehaviorELamp(BlockEntity blockEntity) : base(blockEntity)
        {
            maxConsumption = MyMiniLib.GetAttributeInt(this.Block, "maxConsumption", 4);
        }



        public int maxConsumption;              //максимальное потребление

        public bool isBurned => this.Block.Variant["state"] == "burned";

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
            if (this.Api is { } api)
            {
                int amountInt = (int)Math.Round(amount, MidpointRounding.AwayFromZero);

                if (amountInt != this.LightLevel && this.Block.Variant["state"] != "burned")
                {

                    if (amountInt >= 1 && this.Block.Variant["state"] == "disabled")                               //включаем если питание больше 1
                    {
                        api.World.BlockAccessor.ExchangeBlock(Api.World.GetBlock(Block.CodeWithVariant("state", "enabled")).BlockId, Pos);

                    }
                    else if (amountInt < 1 && this.Block.Variant["state"] == "enabled")                            //гасим если питание меньше 1
                    {
                        api.World.BlockAccessor.ExchangeBlock(Api.World.GetBlock(Block.CodeWithVariant("state", "disabled")).BlockId, Pos);

                    }
                    else
                    {
                        api.World.BlockAccessor.ExchangeBlock(Api.World.GetBlock(this.Blockentity.Block.BlockId).BlockId, Pos);
                    }

                    // в любом случае обновляем значение
                    this.LightLevel = amountInt;
                    this.Blockentity.MarkDirty(true);


                }


            }
        }


        public float getPowerReceive()
        {
            return this.LightLevel;
        }

        public float getPowerRequest()
        {
            return maxConsumption;
        }

        public void Update()
        {
            //смотрим надо ли обновить модельку когда сгорает прибор
            if (this.Api.World.BlockAccessor.GetBlockEntity(this.Blockentity.Pos) is BlockEntityELamp entity && entity.AllEparams != null)
            {

                bool hasBurnout = entity.AllEparams.Any(e => e.burnout);
                if (hasBurnout && entity.Block.Variant["state"] != "burned")
                {
                    string tempK = entity.Block.Variant["tempK"];

                    string[] types = new string[2] { "tempK", "state" };   //типы лампы
                    string[] variants = new string[2] { tempK, "burned" };     //нужный вариант лампы

                    this.Api.World.BlockAccessor.ExchangeBlock(Api.World.GetBlock(Block.CodeWithVariants(types, variants)).BlockId, Pos);


                }

            }



        }


        public override void GetBlockInfo(IPlayer forPlayer, StringBuilder stringBuilder)
        {
            base.GetBlockInfo(forPlayer, stringBuilder);

            //проверяем не сгорел ли прибор
            if (this.Api.World.BlockAccessor.GetBlockEntity(this.Blockentity.Pos) is BlockEntityELamp entity)
            {
                if (isBurned)
                {
                    stringBuilder.AppendLine(Lang.Get("Burned"));
                }
                else
                {
                    stringBuilder.AppendLine(StringHelper.Progressbar(this.LightLevel * 100.0f / maxConsumption));
                    stringBuilder.AppendLine("└ " + Lang.Get("Consumption") + ": " + this.LightLevel + "/" + maxConsumption + " " + Lang.Get("W"));
                }

            }
            stringBuilder.AppendLine();
        }


    }
}
