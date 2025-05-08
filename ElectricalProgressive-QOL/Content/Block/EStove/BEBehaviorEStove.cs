using System.Linq;
using System.Text;
using ElectricalProgressive.Content.Block.ECharger;
using ElectricalProgressive.Content.Block.EHorn;
using ElectricalProgressive.Interface;
using ElectricalProgressive.Utils;
using Vintagestory.API.Common;
using Vintagestory.API.Config;

namespace ElectricalProgressive.Content.Block.EStove;

public class BEBehaviorEStove : BlockEntityBehavior, IElectricConsumer
{
    public int powerSetting;
    private int stoveTemperature;
    public int maxConsumption = 0;
    public BEBehaviorEStove(BlockEntity blockEntity) : base(blockEntity)
    {
        maxConsumption = MyMiniLib.GetAttributeInt(this.Block, "maxConsumption", 100);
    }


    public bool isBurned => this.Block.Variant["state"] == "burned";

    public bool working
    {
        get
        {
            bool w = false;
            BlockEntityEStove? entity = null;
            if (Blockentity is BlockEntityEStove temp)
            {
                entity = temp;
                w = entity.canHeatInput();
                stoveTemperature = (int)entity.stoveTemperature;
            }

            return w;
        }
    }



    public override void GetBlockInfo(IPlayer forPlayer, StringBuilder stringBuilder)
    {
        base.GetBlockInfo(forPlayer, stringBuilder);

        //проверяем не сгорел ли прибор
        if (this.Api.World.BlockAccessor.GetBlockEntity(this.Blockentity.Pos) is BlockEntityEStove entity)
        {

            if (isBurned)
            {
                stringBuilder.AppendLine(Lang.Get("Burned"));
                entity.IsBurning = false;
            }
            else
            {
                stringBuilder.AppendLine(StringHelper.Progressbar(powerSetting * 100.0f / maxConsumption));
                stringBuilder.AppendLine("├ " + Lang.Get("Consumption") + ": " + powerSetting + "/" + maxConsumption + " " + Lang.Get("W"));
                stringBuilder.AppendLine("└ " + Lang.Get("Temperature") + ": " + stoveTemperature + "°");
            }

        }


        stringBuilder.AppendLine();
    }

    public float Consume_request()
    {
        if (working)
            return maxConsumption;
        else
        {
            powerSetting = 0;
            return 0;
        }
    }

    public void Consume_receive(float amount)
    {

        if (!working)
        {
            amount = 0;
        }

        if (powerSetting != amount)
        {
            powerSetting = (int)amount;
        }
    }

    public void Update()
    {
        //смотрим надо ли обновить модельку когда сгорает прибор
        if (this.Api.World.BlockAccessor.GetBlockEntity(this.Blockentity.Pos) is BlockEntityEStove entity && entity.AllEparams != null)
        {
            bool hasBurnout = entity.AllEparams.Any(e => e.burnout);

            if (hasBurnout)
            {
                ParticleManager.SpawnBlackSmoke(this.Api.World, Pos.ToVec3d().Add(0.5, 0.5, 0.5));
            }

            if (hasBurnout && entity.Block.Variant["state"] != "burned")
            {
                string side = entity.Block.Variant["side"];

                string[] types = new string[2] { "state", "side" };   //типы горна
                string[] variants = new string[2] { "burned", side };  //нужный вариант 

                this.Api.World.BlockAccessor.ExchangeBlock(Api.World.GetBlock(Block.CodeWithVariants(types, variants)).BlockId, Pos);
            }
        }
    }

    public float getPowerReceive()
    {
        return this.powerSetting;
    }


    public float getPowerRequest()
    {
        if (working)
            return maxConsumption;
        else
        {
            powerSetting = 0;
            return 0;
        }
    }
}