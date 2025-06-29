using ElectricalProgressive.Interface;
using ElectricalProgressive.Utils;
using System.Linq;
using System.Text;
using Vintagestory.API.Common;
using Vintagestory.API.Config;

namespace ElectricalProgressive.Content.Block.EFreezer2;

public class BEBehaviorEFreezer2 : BEBehaviorBase, IElectricConsumer
{
    public int PowerSetting { get; set; }

    /// <summary>
    /// Максимальное потребление
    /// </summary>
    private readonly int _maxConsumption;

    public BEBehaviorEFreezer2(BlockEntity blockEntity) : base(blockEntity)
    {
        _maxConsumption = MyMiniLib.GetAttributeInt(this.Block, "maxConsumption", 100);
    }

    public void Consume_receive(float amount)
    {
        if (PowerSetting != amount)
            PowerSetting = (int)amount;
    }

    public float Consume_request()
    {
        return _maxConsumption;
    }

    public override void GetBlockInfo(IPlayer forPlayer, StringBuilder stringBuilder)
    {
        base.GetBlockInfo(forPlayer, stringBuilder);

        //проверяем не сгорел ли прибор
        if (this.Api.World.BlockAccessor.GetBlockEntity(this.Blockentity.Pos) is not BlockEntityEFreezer2 entity)
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
        return _maxConsumption;
    }

    public void Update()
    {
        //смотрим надо ли обновить модельку когда сгорает прибор
        if (this.Api.World.BlockAccessor.GetBlockEntity(this.Blockentity.Pos) is not BlockEntityEFreezer2 entity || entity.AllEparams == null)
            return;

        var hasBurnout = entity.AllEparams.Any(e => e.burnout);
        if (hasBurnout)
            ParticleManager.SpawnBlackSmoke(this.Api.World, Pos.ToVec3d().Add(0.1, 1.0, 0.1));

        bool prepareBurnout = entity.AllEparams.Any(e => e.ticksBeforeBurnout > 0);
        if (prepareBurnout)
        {
            ParticleManager.SpawnWhiteSlowSmoke(this.Api.World, Pos.ToVec3d().Add(0.1, 1, 0.1));
        }

        if (!hasBurnout || entity.Block.Variant["state"] == "burned")
            return;

        var type = "state";
        var variant = "burned";
        this.Api.World.BlockAccessor.ExchangeBlock(Api.World.GetBlock(Block.CodeWithVariant(type, variant)).BlockId, Pos);
    }
}