using ElectricalProgressive.Interface;
using ElectricalProgressive.Utils;
using System.Linq;
using System.Text;
using Vintagestory.API.Common;
using Vintagestory.API.Config;

namespace ElectricalProgressive.Content.Block.ECharger;

public class BEBehaviorECharger : BEBehaviorBase, IElectricConsumer
{
    /// <summary>
    /// Мощность в заряднике
    /// </summary>
    public int PowerSetting { get; set; }

    /// <summary>
    /// Максимальное потребление
    /// </summary>
    private readonly int _maxConsumption;

    public BEBehaviorECharger(BlockEntity blockEntity) : base(blockEntity)
    {
        _maxConsumption = MyMiniLib.GetAttributeInt(this.Block, "maxConsumption", 200);
    }

    public bool Working
    {
        get
        {
            var working = false;
            var durability = 0;         //текущая прочность
            var maxDurability = 0;      //максимальная прочность

            if (Blockentity is not BlockEntityECharger entityECharger)
                return working;

            var entityStack = entityECharger.Inventory[0]?.Itemstack;
            if (entityStack is null || entityStack.StackSize == 0)
                return working = false;

            if (entityStack.Item != null &&
                entityStack.Collectible.Attributes["chargable"].AsBool(false)) //предмет?
            {
                durability = entityStack.Attributes.GetInt("durability");
                maxDurability = entityStack.Collectible.GetMaxDurability(entityStack);
                working = durability < maxDurability;
            }
            else if (entityStack.Block is IEnergyStorageItem) //блок?
            {
                durability = entityStack.Attributes.GetInt("durability");
                maxDurability = entityStack.Collectible.GetMaxDurability(entityStack);
                working = durability < maxDurability;
            }

            return working;
        }
    }

    public void Consume_receive(float amount)
    {
        if (!Working)
            amount = 0;

        if (this.PowerSetting != amount)
            this.PowerSetting = (int)amount;
    }

    public float Consume_request()
    {
        if (Working)
            return _maxConsumption;

        return PowerSetting = 0;
    }

    public override void GetBlockInfo(IPlayer forPlayer, StringBuilder stringBuilder)
    {
        base.GetBlockInfo(forPlayer, stringBuilder);

        //проверяем не сгорел ли прибор
        if (this.Api.World.BlockAccessor.GetBlockEntity(this.Blockentity.Pos) is not BlockEntityECharger entity)
            return;

        if (IsBurned)
        {
            return;
        }

        stringBuilder.AppendLine(StringHelper.Progressbar(PowerSetting * 100.0f / _maxConsumption));
        stringBuilder.AppendLine("└ " + Lang.Get("Consumption") + ": " + PowerSetting + "/" + _maxConsumption + " " + Lang.Get("W"));

        stringBuilder.AppendLine();
    }

    public float getPowerReceive()
    {
        return this.PowerSetting;
    }

    public float getPowerRequest()
    {
        if (Working)
            return _maxConsumption;

        return PowerSetting = 0;
    }

    public void Update()
    {
        //смотрим надо ли обновить модельку когда сгорает прибор
        if (this.Api.World.BlockAccessor.GetBlockEntity(this.Blockentity.Pos) is not BlockEntityECharger entity || entity.AllEparams == null)
            return;

        var hasBurnout = entity.AllEparams.Any(e => e.burnout);
        if (hasBurnout)
            ParticleManager.SpawnBlackSmoke(this.Api.World, Pos.ToVec3d().Add(0.5, 0.5, 0.5));

        if (!hasBurnout || entity.Block.Variant["state"] == "burned")
            return;

        var side = entity.Block.Variant["side"];

        var types = new string[2] { "state", "side" };   //типы горна
        var variants = new string[2] { "burned", side };  //нужный вариант 

        this.Api.World.BlockAccessor.ExchangeBlock(Api.World.GetBlock(Block.CodeWithVariants(types, variants)).BlockId, Pos);
    }
}