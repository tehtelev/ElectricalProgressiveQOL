using ElectricalProgressive.Utils;
using System;
using System.Text;
using ElectricalProgressive.Interface;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using System.Linq;
using Vintagestory.API.Util;

namespace ElectricalProgressive.Content.Block.ESFonar
{
    public class BEBehaviorESFonar : BEBehaviorBase, IElectricConsumer
    {
        /// <summary>
        /// Уровень света
        /// </summary>
        public int LightLevel { get; private set; }

        /// <summary>
        /// Заглушка нулевого света
        /// </summary>
        private readonly int[] null_HSV = { 0, 0, 0 };

        /// <summary>
        /// Максимальное потребление
        /// </summary>
        private readonly int _maxConsumption;

        public BEBehaviorESFonar(BlockEntity blockEntity) : base(blockEntity)
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
            if (this.Api.World.BlockAccessor.GetBlockEntity(this.Blockentity.Pos) is BlockEntityESFonar entity)
            {
                if (IsBurned)
                {
                    stringBuilder.AppendLine(Lang.Get("Burned"));
                }
                else
                {
                    stringBuilder.AppendLine(StringHelper.Progressbar(this.LightLevel * 100.0f / _maxConsumption));
                    stringBuilder.AppendLine("└ " + Lang.Get("Consumption") + ": " + this.LightLevel + "/" + _maxConsumption + " " + Lang.Get("W"));
                }
            }

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

            var roundAmount = (int)Math.Round(Math.Min(amount, _maxConsumption), MidpointRounding.AwayFromZero);
            if (roundAmount == LightLevel || Block.Variant["state"] == "burned")
                return;

            if (roundAmount >= 1 && Block.Variant["state"] == "disabled") // включаем если питание больше 1
            {
                Api.World.BlockAccessor.ExchangeBlock(Api.World.GetBlock(Block.CodeWithVariant("state", "enabled")).BlockId, Pos);
            }
            else if (roundAmount < 1 && Block.Variant["state"] == "enabled") // гасим если питание меньше 1
            {
                Api.World.BlockAccessor.ExchangeBlock(Api.World.GetBlock(Block.CodeWithVariant("state", "disabled")).BlockId, Pos);
            }
            else
            {
                Api.World.BlockAccessor.ExchangeBlock(Api.World.GetBlock(Blockentity.Block.BlockId).BlockId, Pos);
            }

            var bufHSV = MyMiniLib.GetAttributeArrayInt(this.Block, "HSV", null_HSV);

            //теперь нужно поделить H и S на 6, чтобы в игре правильно считало цвет
            bufHSV[0] = (int)Math.Round((bufHSV[0] / 6.0), MidpointRounding.AwayFromZero);
            bufHSV[1] = (int)Math.Round((bufHSV[1] / 6.0), MidpointRounding.AwayFromZero);

            //применяем цвет и яркость
            this.Block.LightHsv = new[] {
                (byte)bufHSV[0],
                (byte)bufHSV[1],
                (byte)FloatHelper.Remap(roundAmount, 0, _maxConsumption, 0, bufHSV[2])
            };

            // в любом случае обновляем значение
            LightLevel = roundAmount;
            Blockentity.MarkDirty(true);
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
            if (this.Api.World.BlockAccessor.GetBlockEntity(this.Blockentity.Pos) is not BlockEntityESFonar entity ||
                entity.AllEparams == null)
            {
                return;
            }

            var hasBurnout = entity.AllEparams.Any(e => e.burnout);
            if (hasBurnout)
                ParticleManager.SpawnBlackSmoke(this.Api.World, Pos.ToVec3d().Add(0.5, entity.Block.Variant["height"].ToFloat(), 0.5));

            if (!hasBurnout || entity.Block.Variant["state"] == "burned")
                return;

            var height = entity.Block.Variant["height"];
            var format = entity.Block.Variant["format"];

            var types = new string[3] { "height", "format", "state" };   //типы лампы
            var variants = new string[3] { height, format, "burned" };     //нужный вариант лампы

            this.Api.World.BlockAccessor.ExchangeBlock(Api.World.GetBlock(Block.CodeWithVariants(types, variants)).BlockId, Pos);
        }

        #endregion
    }
}
