using System.Linq;
using System.Text;
using ElectricalProgressive.Content.Block.EHorn;
using ElectricalProgressive.Content.Block.ELamp;
using ElectricalProgressive.Interface;
using ElectricalProgressive.Utils;
using Vintagestory.API.Common;
using Vintagestory.API.Config;

namespace ElectricalProgressive.Content.Block.ECharger;

public class BEBehaviorECharger : BlockEntityBehavior, IElectricConsumer
{
    public int powerSetting;
    public int maxConsumption;


    public BEBehaviorECharger(BlockEntity blockEntity) : base(blockEntity)
    {
        maxConsumption = MyMiniLib.GetAttributeInt(this.Block, "maxConsumption", 200);
    }


    public bool isBurned => this.Block.Variant["state"] == "burned";


    public bool working
    {
        get
        {
            bool w= false;
            int durability;         //текущая прочность
            int maxDurability;      //максимальная прочность

            if (Blockentity is BlockEntityECharger temp)
            {
                ItemStack entityStack = temp.inventory[0]?.Itemstack!;
                if (entityStack?.StackSize > 0)
                {
                    if (entityStack?.Item != null && entityStack.Collectible.Attributes["chargable"].AsBool(false)) //предмет?
                    {
                        durability= entityStack.Attributes.GetInt("durability");
                        maxDurability = entityStack.Collectible.GetMaxDurability(entityStack);
                        w = (durability < maxDurability) ? true : false;
                    }
                    else if (entityStack.Block is IEnergyStorageItem) //блок?
                    {
                        durability = entityStack.Attributes.GetInt("durability");
                        maxDurability = entityStack.Collectible.GetMaxDurability(entityStack);
                        w = (durability < maxDurability) ? true : false;
                    }
                }
                else
                    w = false;
            }

            return w;
        }
    }



    public void Consume_receive(float amount)
    {

        if (!working)
        {
            amount = 0;
        }

        if (this.powerSetting != amount)
        {
            this.powerSetting = (int)amount;
        }

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


    public override void GetBlockInfo(IPlayer forPlayer, StringBuilder stringBuilder)
    {
        base.GetBlockInfo(forPlayer, stringBuilder);

        //проверяем не сгорел ли прибор
        if (this.Api.World.BlockAccessor.GetBlockEntity(this.Blockentity.Pos) is BlockEntityECharger entity)
        {            
            if (isBurned)
            {
                stringBuilder.AppendLine(Lang.Get("Burned"));
            }
            else
            {
                stringBuilder.AppendLine(StringHelper.Progressbar(powerSetting * 100.0f / maxConsumption));
                stringBuilder.AppendLine("└ " + Lang.Get("Consumption") + ": " + powerSetting + "/" + maxConsumption + " " + Lang.Get("W"));
            }
        }
        stringBuilder.AppendLine();
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

    public void Update()
    {
        //смотрим надо ли обновить модельку когда сгорает прибор
        if (this.Api.World.BlockAccessor.GetBlockEntity(this.Blockentity.Pos) is BlockEntityECharger entity && entity.AllEparams != null)
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
}