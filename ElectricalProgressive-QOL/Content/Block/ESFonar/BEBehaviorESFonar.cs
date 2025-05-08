using ElectricalProgressive.Utils;
using System;
using System.Text;
using ElectricalProgressive.Interface;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using System.Linq;

namespace ElectricalProgressive.Content.Block.ESFonar
{
    public class BEBehaviorESFonar : BlockEntityBehavior, IElectricConsumer
    {
        public BEBehaviorESFonar(BlockEntity blockEntity) : base(blockEntity)
        {
            maxConsumption = MyMiniLib.GetAttributeInt(this.Block, "maxConsumption", 4);
        }


        private int[] null_HSV = { 0, 0, 0 };   //заглушка нулевого света
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
            if (Api is { } api)
            {
                int amountInt = (int)Math.Round(Math.Min(amount, maxConsumption), MidpointRounding.AwayFromZero);

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


                    int[] bufHSV = MyMiniLib.GetAttributeArrayInt(this.Block, "HSV", null_HSV);

                    //теперь нужно поделить H и S на 6, чтобы в игре правильно считало цвет
                    bufHSV[0] = (int)Math.Round((bufHSV[0] / 6.0), MidpointRounding.AwayFromZero);
                    bufHSV[1] = (int)Math.Round((bufHSV[1] / 6.0), MidpointRounding.AwayFromZero);


                    //применяем цвет и яркость
                    this.Block.LightHsv = new[] {
                            (byte)bufHSV[0],
                            (byte)bufHSV[1],
                            (byte)FloatHelper.Remap(amountInt, 0, maxConsumption, 0, bufHSV[2])
                        };




                    // в любом случае обновляем значение
                    LightLevel = amountInt;
                    Blockentity.MarkDirty(true);


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
            if (this.Api.World.BlockAccessor.GetBlockEntity(this.Blockentity.Pos) is BlockEntityESFonar entity && entity.AllEparams != null)
            {

                bool hasBurnout = entity.AllEparams.Any(e => e.burnout);

                if (hasBurnout)
                {
                    ParticleManager.SpawnBlackSmoke(this.Api.World, Pos.ToVec3d().Add(0.5, 0.5, 0.5));
                }

                if (hasBurnout && entity.Block.Variant["state"] != "burned")
                {
                    string height = entity.Block.Variant["heght"];
                    string format = entity.Block.Variant["format"];

                    string[] types = new string[3] { "height", "format", "state" };   //типы лампы
                    string[] variants = new string[3] { height, format, "burned" };     //нужный вариант лампы

                    this.Api.World.BlockAccessor.ExchangeBlock(Api.World.GetBlock(Block.CodeWithVariants(types, variants)).BlockId, Pos);


                }

            }



        }


        public override void GetBlockInfo(IPlayer forPlayer, StringBuilder stringBuilder)
        {
            base.GetBlockInfo(forPlayer, stringBuilder);

            //проверяем не сгорел ли прибор
            if (this.Api.World.BlockAccessor.GetBlockEntity(this.Blockentity.Pos) is BlockEntityESFonar entity)
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
