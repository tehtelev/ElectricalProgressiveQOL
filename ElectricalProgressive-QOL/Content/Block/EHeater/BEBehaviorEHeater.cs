using System;
using System.Linq;
using System.Text;
using ElectricalProgressive.Content.Block.ELamp;
using ElectricalProgressive.Interface;
using ElectricalProgressive.Utils;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;

namespace ElectricalProgressive.Content.Block.EHeater
{
    public class BEBehaviorEHeater : BlockEntityBehavior, IElectricConsumer
    {
        public BEBehaviorEHeater(BlockEntity blockEntity) : base(blockEntity)
        {
            maxConsumption = MyMiniLib.GetAttributeInt(this.Block, "maxConsumption", 4);
        }

        public int maxConsumption;              //максимальное потребление

        public bool isBurned => this.Block.Variant["state"] == "burned";

        public int HeatLevel { get; private set; }


        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            base.ToTreeAttributes(tree);
            tree.SetInt("electricalprogressive:HeatLevel", HeatLevel);

        }

        public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldAccessForResolve)
        {
            base.FromTreeAttributes(tree, worldAccessForResolve);
            HeatLevel = tree.GetInt("electricalprogressive:HeatLevel");

        }




        public void Consume_receive(float amount)
        {
            if (this.Api is { } api)
            {
                if ((int)Math.Round(amount, MidpointRounding.AwayFromZero) != this.HeatLevel && this.Block.Variant["status"] != "burned")
                {

                    if ((int)Math.Round(amount, MidpointRounding.AwayFromZero) >= 1 && this.Block.Variant["state"] == "disabled")                               //включаем если питание больше 1
                    {
                        api.World.BlockAccessor.ExchangeBlock(Api.World.GetBlock(Block.CodeWithVariant("state", "enabled")).BlockId, Pos);
                    }
                    else if ((int)Math.Round(amount, MidpointRounding.AwayFromZero) < 1 && this.Block.Variant["state"] == "enabled")                            //гасим если питание меньше 1
                    {
                        api.World.BlockAccessor.ExchangeBlock(Api.World.GetBlock(Block.CodeWithVariant("state", "disabled")).BlockId, Pos);
                    }


                    this.HeatLevel = (int)Math.Round(amount, MidpointRounding.AwayFromZero);
                    this.Blockentity.MarkDirty(true);
                }
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
            if (this.Api.World.BlockAccessor.GetBlockEntity(this.Blockentity.Pos) is BlockEntityEHeater entity)
            {
                if (isBurned)
                {
                    stringBuilder.AppendLine(Lang.Get("Burned"));
                }
                else
                {
                    stringBuilder.AppendLine(StringHelper.Progressbar(this.HeatLevel * 100.0f / maxConsumption));
                    stringBuilder.AppendLine("└ " + Lang.Get("Consumption") + ": " + this.HeatLevel + "/" + maxConsumption + " " + Lang.Get("W"));
                }
            }
            stringBuilder.AppendLine();
        }



        public float getPowerReceive()
        {
            return this.HeatLevel;
        }

        public float getPowerRequest()
        {
            return maxConsumption;
        }

        public void Update()
        {
            //смотрим надо ли обновить модельку когда сгорает прибор
            if (this.Api.World.BlockAccessor.GetBlockEntity(this.Blockentity.Pos) is BlockEntityEHeater entity && entity.AllEparams != null)
            {
                bool hasBurnout = entity.AllEparams.Any(e => e.burnout);

                if (hasBurnout)
                {
                    ParticleManager.SpawnBlackSmoke(this.Api.World, Pos.ToVec3d().Add(0.5, 0.5, 0.5));
                }

                if (hasBurnout && entity.Block.Variant["state"] != "burned")
                {
                    string[] types = new string[1] {"state"};   //типы лампы
                    string[] variants = new string[1] {"burned"};     //нужный вариант лампы

                    this.Api.World.BlockAccessor.ExchangeBlock(Api.World.GetBlock(Block.CodeWithVariants(types, variants)).BlockId, Pos);
                }
            }
        }
    }
}
