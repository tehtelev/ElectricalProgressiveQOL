using ElectricalProgressive.Utils;
using System;
using System.Text;
using ElectricalProgressive.Interface;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using System.Linq;

namespace ElectricalProgressive.Content.Block.ELamp
{
    public class BEBehaviorELamp : BEBehaviorBase, IElectricConsumer
    {
        /// <summary>
        /// Уровень света
        /// </summary>
        public int LightLevel { get; private set; }

        /// <summary>
        /// Заглушка нулевого света
        /// </summary>
        //private int[] null_HSV = { 0, 0, 0 };

        /// <summary>
        /// Максимальное потребление
        /// </summary>
        private readonly int _maxConsumption;

        public BEBehaviorELamp(BlockEntity blockEntity) : base(blockEntity)
        {
            _maxConsumption = MyMiniLib.GetAttributeInt(this.Block, "maxConsumption", 4);
        }

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

        public override void GetBlockInfo(IPlayer forPlayer, StringBuilder stringBuilder)
        {
            base.GetBlockInfo(forPlayer, stringBuilder);

            //проверяем не сгорел ли прибор
            if (this.Api.World.BlockAccessor.GetBlockEntity(this.Blockentity.Pos) is not BlockEntityELamp entity)
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

            stringBuilder.AppendLine(StringHelper.Progressbar(this.LightLevel * 100.0f / _maxConsumption));
            stringBuilder.AppendLine("└ " + Lang.Get("Consumption") + ": " + this.LightLevel + "/" + _maxConsumption + " " + Lang.Get("W"));

            stringBuilder.AppendLine();
        }

        #region IElectricConsumer

        public float Consume_request()
        {
            return _maxConsumption;
        }

        public void Consume_receive(float amount)
        {
            if (Api is null)
                return;

            int roundAmount = (int)Math.Round(Math.Min(amount, _maxConsumption), MidpointRounding.AwayFromZero);
            if (roundAmount == LightLevel || Block.Variant["state"] == "burned")
                return;


            // включаем если питание больше 25%
            if (roundAmount * 4 >= _maxConsumption && Block.Variant["state"] == "disabled")
            {
                Api.World.BlockAccessor.ExchangeBlock(Api.World.GetBlock(Block.CodeWithVariant("state", "enabled")).BlockId, Pos);
                Blockentity.MarkDirty(true);
            }
            // гасим если питание меньше 1
            else if (roundAmount * 4 < _maxConsumption && Block.Variant["state"] == "enabled")
            {
                Api.World.BlockAccessor.ExchangeBlock(Api.World.GetBlock(Block.CodeWithVariant("state", "disabled")).BlockId, Pos);
                Blockentity.MarkDirty(true);
            }




            // в любом случае обновляем значение
            LightLevel = roundAmount;

        }

        public float getPowerReceive()
        {
            return this.LightLevel;
        }

        public float getPowerRequest()
        {
            return _maxConsumption;
        }

        public void Update()
        {
            //смотрим надо ли обновить модельку когда сгорает прибор
            if (this.Api.World.BlockAccessor.GetBlockEntity(this.Blockentity.Pos) is not BlockEntityELamp entity ||
                entity.AllEparams == null)
            {
                return;
            }

            var hasBurnout = entity.AllEparams.Any(e => e.burnout);
            if (hasBurnout)
                ParticleManager.SpawnBlackSmoke(this.Api.World, Pos.ToVec3d().Add(0.5, 0.3, 0.5));

            if (!hasBurnout || entity.Block.Variant["state"] == "burned")
                return;

            var tempK = entity.Block.Variant["tempK"];

            var types = new string[2] { "tempK", "state" };   //типы лампы
            var variants = new string[2] { tempK, "burned" };     //нужный вариант лампы

            this.Api.World.BlockAccessor.ExchangeBlock(Api.World.GetBlock(Block.CodeWithVariants(types, variants)).BlockId, Pos);
        }

        #endregion
    }
}
