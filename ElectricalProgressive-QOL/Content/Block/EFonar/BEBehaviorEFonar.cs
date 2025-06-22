using ElectricalProgressive.Utils;
using System;
using System.Text;
using ElectricalProgressive.Interface;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using System.Linq;

namespace ElectricalProgressive.Content.Block.EFonar
{
    public class BEBehaviorEFonar : BEBehaviorBase, IElectricConsumer
    {
        /// <summary>
        /// Уровень света
        /// </summary>
        public int LightLevel { get; private set; }

        /// <summary>
        /// Максимальное потребление
        /// </summary>
        private readonly int _maxConsumption;

        /// <summary>
        /// Заглушка нулевого света
        /// </summary>
        //private readonly int[] null_HSV = { 0, 0, 0 };

        public BEBehaviorEFonar(BlockEntity blockEntity) : base(blockEntity)
        {
            _maxConsumption = MyMiniLib.GetAttributeInt(Block, "maxConsumption", 4);
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

        public float Consume_request()
        {
            return _maxConsumption;
        }

        public void Consume_receive(float amount)
        {
            if (Api is null)
                return;

            int roundAmount = (int)Math.Round(Math.Min(amount, _maxConsumption), MidpointRounding.AwayFromZero);

            if (roundAmount == LightLevel || IsBurned) //если сгорело или не поменялось
                return;

            //включаем если питание больше 25 %
            if (roundAmount*4>= _maxConsumption && Block.Variant["state"] == "disabled")
            {
                Api.World.BlockAccessor.ExchangeBlock(Api.World.GetBlock(Block.CodeWithVariant("state", "enabled")).BlockId, Pos);
                Blockentity.MarkDirty(true);
            }
            //гасим если питание меньше 25 %
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
            return LightLevel;
        }

        public float getPowerRequest()
        {
            return _maxConsumption;
        }

        public void Update()
        {
            //смотрим надо ли обновить модельку когда сгорает прибор
            if (Api.World.BlockAccessor.GetBlockEntity(Blockentity.Pos) is not BlockEntityEFonar entity ||
                entity.AllEparams == null)
            {
                return;
            }

            var hasBurnout = entity.AllEparams.Any(e => e.burnout);
            if (hasBurnout)
                ParticleManager.SpawnBlackSmoke(this.Api.World, Pos.ToVec3d().Add(0.5, 0.5, 0.5));

            if (hasBurnout && entity.Block.Variant["state"] != "burned")
                Api.World.BlockAccessor.ExchangeBlock(Api.World.GetBlock(Block.CodeWithVariant("state", "burned")).BlockId, Pos);
        }


        public override void GetBlockInfo(IPlayer forPlayer, StringBuilder stringBuilder)
        {
            base.GetBlockInfo(forPlayer, stringBuilder);

            //проверяем не сгорел ли прибор
            if (Api.World.BlockAccessor.GetBlockEntity(Blockentity.Pos) is BlockEntityEFonar entity)
            {
                if (IsBurned)
                {
                    stringBuilder.AppendLine(Lang.Get("Burned"));
                }
                else
                {
                    stringBuilder.AppendLine(StringHelper.Progressbar(LightLevel * 100.0f / _maxConsumption));
                    stringBuilder.AppendLine("└ " + Lang.Get("Consumption") + ": " + LightLevel + "/" + _maxConsumption + " " + Lang.Get("W"));
                }

            }
            stringBuilder.AppendLine();
        }
    }
}
