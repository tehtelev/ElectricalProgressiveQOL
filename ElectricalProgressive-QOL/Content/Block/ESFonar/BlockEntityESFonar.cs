using System;
using System.Linq;
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
                  
                        this.ElectricalProgressive!.Connection = value;
                        this.facing = value;

                }
            }
        }



        public bool IsEnabled => this.Behavior.LightLevel >= 1;





        public override void OnBlockPlaced(ItemStack? byItemStack = null)
        {
            base.OnBlockPlaced(byItemStack);

            if (this.ElectricalProgressive == null || byItemStack == null)
                return;



            //задаем параметры блока/проводника
            var voltage = MyMiniLib.GetAttributeInt(byItemStack!.Block, "voltage", 32);
            var maxCurrent = MyMiniLib.GetAttributeFloat(byItemStack!.Block, "maxCurrent", 5.0F);
            var isolated = MyMiniLib.GetAttributeBool(byItemStack!.Block, "isolated", false);

            this.ElectricalProgressive!.Connection = Facing.DownAll;
            this.ElectricalProgressive.Eparams = (
                new EParams(voltage, maxCurrent, "", 0, 1, 1, false, isolated),
                FacingHelper.Faces(Facing.DownAll).First().Index);

        }

    }
}

