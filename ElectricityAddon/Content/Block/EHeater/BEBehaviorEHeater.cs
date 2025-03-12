using System.Text;
using ElectricityAddon.Interface;
using ElectricityAddon.Utils;
using Vintagestory.API.Common;
using Vintagestory.API.Config;

namespace ElectricityAddon.Content.Block.EHeater
{
    public class BEBehaviorEHeater : BlockEntityBehavior, IElectricConsumer
    {
        public BEBehaviorEHeater(BlockEntity blockEntity) : base(blockEntity)
        {
        }

        public int HeatLevel { get; private set; }



        public void Consume(int heatLevel)
        {
            if (this.Api is { } api)
            {
                if (heatLevel != this.HeatLevel)
                {
                    switch (this.HeatLevel)
                    {
                        case 0 when heatLevel > 0:
                            {
                                var assetLocation = this.Blockentity.Block.CodeWithVariant("state", "enabled");
                                var block = api.World.BlockAccessor.GetBlock(assetLocation);
                                api.World.BlockAccessor.ExchangeBlock(block.Id, this.Blockentity.Pos);
                                break;
                            }
                        case > 0 when heatLevel == 0:
                            {
                                var assetLocation = this.Blockentity.Block.CodeWithVariant("state", "disabled");
                                var block = api.World.BlockAccessor.GetBlock(assetLocation);
                                api.World.BlockAccessor.ExchangeBlock(block.Id, this.Blockentity.Pos);
                                break;
                            }
                    }

                    this.Blockentity.Block.LightHsv = new[] {
                        (byte)FloatHelper.Remap(heatLevel, 0, 32, 0, 8),
                        (byte)FloatHelper.Remap(heatLevel, 0, 32, 0, 2),
                        (byte)FloatHelper.Remap(heatLevel, 0, 32, 0, 21)
                    };

                    this.HeatLevel = heatLevel;
                    this.Blockentity.MarkDirty(true);
                }
            }
        }

        public void Consume_receive(float amount)
        {
            throw new System.NotImplementedException();
        }

        public float Consume_request()
        {
            throw new System.NotImplementedException();
        }

        public override void GetBlockInfo(IPlayer forPlayer, StringBuilder stringBuilder)
        {
            base.GetBlockInfo(forPlayer, stringBuilder);

            stringBuilder.AppendLine(StringHelper.Progressbar(this.HeatLevel * 100.0f / 8.0f));
            stringBuilder.AppendLine("└ " + Lang.Get("Consumption") + this.HeatLevel + "/" + 8 + "Eu");
            stringBuilder.AppendLine();
        }

        public float getPowerReceive()
        {
            throw new System.NotImplementedException();
        }

        public float getPowerRequest()
        {
            throw new System.NotImplementedException();
        }

        public void Update()
        {
            throw new System.NotImplementedException();
        }
    }
}
