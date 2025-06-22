using ElectricalProgressive.Interface;
using ElectricalProgressive.Utils;
using System.Linq;
using System.Text;
using Vintagestory.API.Common;
using Vintagestory.API.Config;

namespace ElectricalProgressive.Content.Block.EOven;

public class BEBehaviorEOven : BEBehaviorBase, IElectricConsumer
{
    public int PowerSetting { get; set; }

    /// <summary>
    /// Температура печи
    /// </summary>
    private float _ovenTemperature;

    /// <summary>
    /// Максимальное потребление
    /// </summary>
    private readonly int _maxConsumption;

    public BEBehaviorEOven(BlockEntity blockEntity) : base(blockEntity)
    {
        _maxConsumption = MyMiniLib.GetAttributeInt(this.Block, "maxConsumption", 100);
    }

    public bool Working
    {
        get
        {
            var working = false;
            if (Blockentity is not BlockEntityEOven entity)
                return working;

            _ovenTemperature = (int)entity.ovenTemperature;

            //проверяем количество занятых слотов и готовой еды
            var stack_count = 0;
            var stack_count_perfect = 0;

            for (var index = 0; index < entity.bakeableCapacity; ++index)
            {
                var itemstack = entity.ovenInv[index].Itemstack;
                if (itemstack == null)
                    continue;

                if (itemstack.Class == EnumItemClass.Block)
                {
                    var blockCode = itemstack.Block.Code.ToString();
                    if (blockCode.Contains("perfect") || blockCode.Contains("charred"))
                        stack_count_perfect++;
                }
                else
                {
                    var itemCode = itemstack.Item.Code.ToString();
                    if (itemCode.Contains("perfect") || itemCode.Contains("rot") || itemCode.Contains("charred"))
                        stack_count_perfect++;
                }

                stack_count++;
            }

            if (stack_count > 0)   // если еда есть - греем печку
            {
                // если еда вся готова - не греем
                working = stack_count_perfect != stack_count;
            }
            else                      // если еды нет - не греем
                working = false;

            return working;
        }
    }


    public override void GetBlockInfo(IPlayer forPlayer, StringBuilder stringBuilder)
    {
        base.GetBlockInfo(forPlayer, stringBuilder);

        //проверяем не сгорел ли прибор
        if (this.Api.World.BlockAccessor.GetBlockEntity(this.Blockentity.Pos) is BlockEntityEOven entity)
        {
            if (IsBurned)
            {
                stringBuilder.AppendLine(Lang.Get("Burned"));
            }
            else
            {
                stringBuilder.AppendLine(StringHelper.Progressbar(PowerSetting * 100.0f / _maxConsumption));
                stringBuilder.AppendLine("├ " + Lang.Get("Consumption") + ": " + PowerSetting + "/" + _maxConsumption + " " + Lang.Get("W"));
                stringBuilder.AppendLine("└ " + Lang.Get("Temperature") + ": " + ((int)_ovenTemperature).ToString() + "°");
            }
        }

        stringBuilder.AppendLine();
    }

    #region IElectricConsumer

    public float Consume_request()
    {
        if (Working)
            return _maxConsumption;

        return PowerSetting = 0;
    }

    public void Consume_receive(float amount)
    {
        if (!Working)
            amount = 0;

        if (PowerSetting != amount)
            PowerSetting = (int)amount;
    }

    public void Update()
    {
        //смотрим надо ли обновить модельку когда сгорает прибор
        if (this.Api.World.BlockAccessor.GetBlockEntity(this.Blockentity.Pos) is not BlockEntityEOven entity ||
            entity.AllEparams == null)
        {
            return;
        }

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

    #endregion
}