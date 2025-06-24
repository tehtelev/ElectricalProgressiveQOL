using ElectricalProgressive.Interface;
using ElectricalProgressive.Utils;
using System;
using System.Linq;
using System.Text;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;

namespace ElectricalProgressive.Content.Block.EHeater
{
    public class BEBehaviorEHeater : BEBehaviorBase, IElectricConsumer
    {
        public int HeatLevel { get; private set; }

        /// <summary>
        /// Максимальное потребление
        /// </summary>
        private readonly int _maxConsumption;

        public BEBehaviorEHeater(BlockEntity blockEntity) : base(blockEntity)
        {
            _maxConsumption = MyMiniLib.GetAttributeInt(this.Block, "maxConsumption", 4);
        }

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

        public override void GetBlockInfo(IPlayer forPlayer, StringBuilder stringBuilder)
        {
            base.GetBlockInfo(forPlayer, stringBuilder);

            //проверяем не сгорел ли прибор
            if (this.Api.World.BlockAccessor.GetBlockEntity(this.Blockentity.Pos) is not BlockEntityEHeater entity)
                return;

            if (IsBurned)
            {
                // выясняем причину сгорания (надо куда-то вынести сей кусочек)
                string cause = "";
                if (entity.AllEparams.Any(e => e.causeBurnout == 1))
                {
                    cause = ElectricalProgressiveBasics.causeBurn[1];
                }
                else if (entity.AllEparams.Any(e => e.causeBurnout == 2))
                {
                    cause = ElectricalProgressiveBasics.causeBurn[2];
                }
                else if (entity.AllEparams.Any(e => e.causeBurnout == 3))
                {
                    cause = ElectricalProgressiveBasics.causeBurn[3];
                }

                stringBuilder.AppendLine(Lang.Get("Burned") + " " + cause);
                return;
            }

            stringBuilder.AppendLine(StringHelper.Progressbar(this.HeatLevel * 100.0f / _maxConsumption));
            stringBuilder.AppendLine("└ " + Lang.Get("Consumption") + ": " + this.HeatLevel + "/" + _maxConsumption + " " + Lang.Get("W"));

            stringBuilder.AppendLine();
        }

        #region IElectricConsumer

        public float Consume_request()
        {
            return _maxConsumption;
        }

        public void Consume_receive(float amount)
        {
            if (this.Api is not { } api)
                return;

            var roundAmount = (int)Math.Round(amount, MidpointRounding.AwayFromZero);
            if (roundAmount == this.HeatLevel || this.Block.Variant["state"] == "burned")
                return;

            // включаем если питание больше 1
            if (roundAmount >= 1 && this.Block.Variant["state"] == "disabled")
            {
                api.World.BlockAccessor.ExchangeBlock(api.World.GetBlock(Block.CodeWithVariant("state", "enabled")).BlockId, Pos);
            }
            // гасим если питание меньше 1
            else if (roundAmount < 1 && this.Block.Variant["state"] == "enabled")
            {
                api.World.BlockAccessor.ExchangeBlock(api.World.GetBlock(Block.CodeWithVariant("state", "disabled")).BlockId, Pos);
            }

            this.HeatLevel = roundAmount;
            this.Blockentity.MarkDirty(true);
        }

        public void Update()
        {
            //смотрим надо ли обновить модельку когда сгорает прибор
            if (this.Api.World.BlockAccessor.GetBlockEntity(this.Blockentity.Pos) is not BlockEntityEHeater entity || entity.AllEparams == null)
                return;

            var hasBurnout = entity.AllEparams.Any(e => e.burnout);
            if (hasBurnout)
                ParticleManager.SpawnBlackSmoke(this.Api.World, Pos.ToVec3d().Add(0.5, 0.5, 0.5));

            if (!hasBurnout || entity.Block.Variant["state"] == "burned")
                return;

            var types = new string[1] { "state" };   //типы лампы
            var variants = new string[1] { "burned" };     //нужный вариант лампы

            this.Api.World.BlockAccessor.ExchangeBlock(Api.World.GetBlock(Block.CodeWithVariants(types, variants)).BlockId, Pos);
        }

        public float getPowerReceive()
        {
            return this.HeatLevel;
        }

        public float getPowerRequest()
        {
            return _maxConsumption;
        }

        #endregion
    }
}
