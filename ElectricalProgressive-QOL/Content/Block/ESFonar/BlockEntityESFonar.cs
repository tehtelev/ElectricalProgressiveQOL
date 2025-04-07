using System;
using ElectricalProgressive.Utils;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Util;

namespace ElectricalProgressive.Content.Block.ESFonar
{
    internal class BlockEntityESFonar : BlockEntity
    {
        private Facing facing = Facing.None;

        private BEBehaviorElectricalProgressive? ElectricalProgressive => GetBehavior<BEBehaviorElectricalProgressive>();

        private BEBehaviorESFonar Behavior => this.GetBehavior<BEBehaviorESFonar>();

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
            get => this.facing;
            set
            {
                if (value != this.facing)
                {

                        //если лампа обычная
                        this.ElectricalProgressive!.Connection = FacingHelper.FullFace(this.facing = value);  
                   
                }
            }
        }

        public bool IsEnabled => this.Behavior.LightLevel >= 1;

        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            base.ToTreeAttributes(tree);

            tree.SetBytes("electricalprogressive:facing", SerializerUtil.Serialize(this.facing));
        }

        public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldAccessForResolve)
        {
            base.FromTreeAttributes(tree, worldAccessForResolve);

            try
            {
                this.facing = SerializerUtil.Deserialize<Facing>(tree.GetBytes("electricalprogressive:facing"));
            }
            catch (Exception exception)
            {
                this.Api?.Logger.Error(exception.ToString());
            }
        }
    }
}

