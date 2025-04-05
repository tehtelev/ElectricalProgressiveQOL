using System;
using ElectricalProgressive.Utils;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Util;

namespace ElectricalProgressive.Content.Block.EFonar
{
    internal class BlockEntityEFonar : BlockEntity
    {
        private Facing facing = Facing.None;

        private BEBehaviorElectricalProgressive? ElectricalProgressive => GetBehavior<BEBehaviorElectricalProgressive>();

        private BEBehaviorEFonar Behavior => GetBehavior<BEBehaviorEFonar>();

        //передает значения из Block в BEBehaviorElectricalProgressive
        public (EParams, int) Eparams
        {
            get => this.ElectricalProgressive?.Eparams ?? (new EParams(), 0);
            set => this.ElectricalProgressive!.Eparams = value;
        }

        //передает значения из Block в BEBehaviorElectricalProgressive
        public EParams[] AllEparams
        {
            get => this.ElectricalProgressive?.AllEparams ?? new EParams[]
                        {
                        new EParams(),
                        new EParams(),
                        new EParams(),
                        new EParams(),
                        new EParams(),
                        new EParams()
                        };
            set
            {
                if (this.ElectricalProgressive != null)
                {
                    this.ElectricalProgressive.AllEparams = value;
                }
            }
        }

        public Facing Facing
        {
            get => facing;
            set
            {
                if (value != facing)
                {                
                        ElectricalProgressive!.Connection = value;
                        facing = value;
                }
            }
        }

        public bool IsEnabled => Behavior.LightLevel >= 1;

        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            base.ToTreeAttributes(tree);

            tree.SetBytes("electricalprogressive:facing", SerializerUtil.Serialize(facing));
        }

        public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldAccessForResolve)
        {
            base.FromTreeAttributes(tree, worldAccessForResolve);

            try
            {
                facing = SerializerUtil.Deserialize<Facing>(tree.GetBytes("electricalprogressive:facing"));
            }
            catch (Exception exception)
            {
                Api?.Logger.Error(exception.ToString());
            }
        }
    }
}

