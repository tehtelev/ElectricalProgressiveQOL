using System;
using ElectricalProgressive.Utils;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

namespace ElectricalProgressive.Content.Block.EHeater {
    public class BlockEntityEHeater : BlockEntity, IHeatSource {
        private Facing facing = Facing.None;

        private BEBehaviorElectricalProgressive? ElectricalProgressive => GetBehavior<BEBehaviorElectricalProgressive>();

        private BEBehaviorEHeater Behavior => this.GetBehavior<BEBehaviorEHeater>();


        public Facing Facing {
            get => this.facing;
            set {
                if (value != this.facing) {
                    this.ElectricalProgressive.Connection =
                        FacingHelper.FullFace(this.facing = value);
                }
            }
        }

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

        public bool IsEnabled => this.Behavior?.HeatLevel >= 1;

        /// <summary>
        /// Отвечает за тепло отдаваемое в окружающую среду
        /// </summary>
        /// <param name="world"></param>
        /// <param name="heatSourcePos"></param>
        /// <param name="heatReceiverPos"></param>
        /// <returns></returns>
        public float GetHeatStrength(IWorldAccessor world, BlockPos heatSourcePos, BlockPos heatReceiverPos) {
            if (this.Behavior == null)
                return 0.0f;
            else
                return this.Behavior.HeatLevel / this.Behavior.getPowerRequest() * MyMiniLib.GetAttributeFloat(this.Block, "maxHeat", 0.0F); 
        }
        

        public override void ToTreeAttributes(ITreeAttribute tree) {
            base.ToTreeAttributes(tree);

            tree.SetBytes("electricalprogressive:facing", SerializerUtil.Serialize(this.facing));
        }


        public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldAccessForResolve) {
            base.FromTreeAttributes(tree, worldAccessForResolve);

            try {
                this.facing = SerializerUtil.Deserialize<Facing>(tree.GetBytes("electricalprogressive:facing"));
            }
            catch (Exception exception) {
                this.Api?.Logger.Error(exception.ToString());
            }
        }
    }
}
