using System;
using System.Linq;
using System.Text;
using ElectricityAddon.Interface;
using ElectricityAddon.Utils;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent.Mechanics;


namespace ElectricityAddon.Content.Block.EMotor;

public class BEBehaviorEMotorTier1 : BEBehaviorMPBase, IElectricConsumer
{

    private static CompositeShape? compositeShape;

    private float powerRequest = I_max;         // Нужно энергии
    private float powerReceive = 0;             // Дали энергии

    // Константы двигателя
    private static float I_min;                 // Минимальный ток
    private static float I_max;                 // Максимальный ток
    private static float torque_max;            // Максимальный крутящий момент
    private static float kpd_max;               // Пиковый КПД
    private static float speed_max;             // Максимальная скорость вращения
    private static float resistance_factor;     // множитель сопротивления

    private float torque;                       // Текущий крутящий момент
    private float I_value, I_value2;            // Ток потребления
    public float kpd;                           // КПД

    private float[] def_Params = { 10.0F, 100.0F, 0.5F, 0.75F, 0.5F, 0.1F };   //заглушка
    public float[] Params = { 0, 0, 0, 0, 0, 0 };                              //сюда берем параметры из ассетов

    private static float constanta = (I_max - I_min) / torque_max;

    /// <summary>
    /// Извлекаем параметры из ассетов
    /// </summary>
    public void GetParams()
    {
        Params = MyMiniLib.GetAttributeArrayFloat(this.Block, "params", def_Params);
        I_min = Params[0];
        I_max = Params[1];
        torque_max = Params[2];
        kpd_max = Params[3];
        speed_max = Params[4];
        resistance_factor = Params[5];
    }

    public BEBehaviorEMotorTier1(BlockEntity blockEntity) : base(blockEntity)
    {
        GetParams();
    }




    public override BlockFacing OutFacingForNetworkDiscovery
    {
        get
        {
            if (this.Blockentity is BlockEntityEMotor entity && entity.Facing != Facing.None)
            {
                return FacingHelper.Directions(entity.Facing).First();
            }

            return BlockFacing.NORTH;
        }
    }


    public override int[] AxisSign => this.OutFacingForNetworkDiscovery.Index switch
    {
        0 => new[]
        {
            +0,
            +0,
            -1
        },
        1 => new[]
        {
            -1,
            +0,
            +0
        },
        2 => new[]
        {
            +0,
            +0,
            -1
        },
        3 => new[]
        {
            -1,
            +0,
            +0
        },
        4 => new[]
        {
            +0,
            +1,
            +0
        },
        5 => new[]
        {
            +0,
            +1,
            +0
        },
        _ => throw new Exception()
    };

    /// <summary>
    /// Диапазон потребления
    /// </summary>
    public ConsumptionRange ConsumptionRange => new(0, (int)I_max);

    public BlockPos Pos => this.Position;

    /// <summary>
    /// Запрашивает энергию
    /// </summary>
    public float Consume_request()
    {
        return this.powerRequest;
    }

    /// <summary>
    /// Получает энергию
    /// </summary>
    public void Consume_receive(float amount)
    {
        if (this.powerReceive != amount)
        {
            this.powerReceive = amount;
            this.Blockentity.MarkDirty(true);
        }
    }


    public void Consume(int amount)                //удалить можно---------------------------
    {

    }




    // не удалять
    // никто не обращается к этой функции, когда работает GetTorque, но быть должна
    public override float GetResistance()
    {
        return 0;
    }


    /// <summary>
    /// Считаем сопротивление самого двигателя
    /// </summary>
    public float Resistance(float spd)
    {
        return (Math.Abs(spd) > speed_max)                                          // Если скорость превышает максимальную, рассчитываем сопротивление как степенную зависимость
            ? resistance_factor * (float)Math.Pow((Math.Abs(spd) / speed_max), 2f)  // Степенная зависимость, если скорость ушла за пределы двигателя   
            : resistance_factor * Math.Abs(spd) / speed_max;                        // Линейное сопротивление для обычных скоростей
    }

    /// <summary>
    /// Рассчитываем КПД
    /// </summary>
    public float KPD(float tor)
    {
        float b = 0.7f;                         // Положение вершины параболы
        float a = (tor <= torque_max / 2.0F)    // Левая и права ветвь параболы разные
            ? 2.04F
            : 0.8f;
        float buf = kpd_max * (1 - a * (float)Math.Pow(tor / torque_max - b, 2));   // Параболическая зависимость
        return Math.Max(0.01f, buf);                                                // Минимальное значение КПД
    }




    /// <summary>
    /// Основной метод поведения двигателя возвращающий момент и сопротивление
    /// </summary>
    public override float GetTorque(long tick, float speed, out float resistance)
    {

        torque = 0f;                            // Текущий крутящий момент
        resistance = Resistance(speed);         // Вычисляем текущее сопротивление двигателя    
        I_value = I_min;                        // Ток потребления

        float I_amount = this.powerReceive;     // Доступно тока/энергии 

        if (I_amount < I_min)                   // Если ток меньше минимального, двигатель не работает
            return torque;

        I_value = Math.Min(I_amount, I_max);    // Берем, что дают


        torque = Math.Min(Network.NetworkResistance, torque_max);           // Рассчитываем момент для компенсации сопротивления
        float torque2 = torque_max * (I_value - I_min) / (I_max - I_min);   // Рассчитываем момент линейно от тока
        torque = (torque + torque2) / 2;                                    // Выдаем момент среднее между вычисленных

        //torque = torque_max * (I_value - I_min) / (I_max - I_min);        // Берем максимум момента из всей энергии, что нам дают

        I_value = torque * constanta / KPD(torque) + I_min;                 // Ток потребления с учетом КПД


        float torque_down = 0;                                              
        int k = 0;
        I_value2 = I_value;
        while (I_value2 > I_value)                                          // Проверка, чтобы ток не превышал максимальное значение I_max и I_amount
        {
            k++;
            // Пропорционально снижаем крутящий момент
            torque_down = torque * (1 - (0.02F * k));                       // Уменьшаем крутящий момент на 2%

            if (torque_down < 0)
            {
                torque_down = 0;
                break;
            }
            
            I_value2 = torque_down * constanta / KPD(torque_down) + I_min;  // Ток потребления с учетом КПД

        }

        if (k > 0)
            torque = torque_down;                                           // Отдаем новое значение момента
           

        this.powerRequest = I_value;                                        // Запрашиваем энергии столько, сколько нужно реально для работы


        return this.propagationDir == this.OutFacingForNetworkDiscovery     // Возвращаем все значения
            ? 1f * torque
            : -1f * torque;
    }


    public override void WasPlaced(BlockFacing connectedOnFacing)
    {
    }


    protected override CompositeShape? GetShape()
    {
        if (this.Api is { } api && this.Blockentity is BlockEntityEMotor entity && entity.Facing != Facing.None)
        {
            var direction = this.OutFacingForNetworkDiscovery;

            if (BEBehaviorEMotorTier1.compositeShape == null)
            {
                var location = this.Block.CodeWithVariant("type", "rotor");
                BEBehaviorEMotorTier1.compositeShape = api.World.BlockAccessor.GetBlock(location).Shape.Clone();
            }

            var shape = BEBehaviorEMotorTier1.compositeShape.Clone();

            if (direction == BlockFacing.NORTH)
            {
                shape.rotateY = 0;
            }

            if (direction == BlockFacing.EAST)
            {
                shape.rotateY = 270;
            }

            if (direction == BlockFacing.SOUTH)
            {
                shape.rotateY = 180;
            }

            if (direction == BlockFacing.WEST)
            {
                shape.rotateY = 90;
            }

            if (direction == BlockFacing.UP)
            {
                shape.rotateX = 90;
            }

            if (direction == BlockFacing.DOWN)
            {
                shape.rotateX = 270;
            }

            return shape;
        }

        return null;
    }

    protected override void updateShape(IWorldAccessor worldForResolve)
    {
        this.Shape = this.GetShape();
    }

    public override bool OnTesselation(ITerrainMeshPool mesher, ITesselatorAPI tesselator)
    {
        return false;
    }


    /// <summary>
    /// Подсказка при наведении на блок
    /// </summary>
    public override void GetBlockInfo(IPlayer forPlayer, StringBuilder stringBuilder)
    {
        base.GetBlockInfo(forPlayer, stringBuilder);

        stringBuilder.AppendLine(StringHelper.Progressbar(powerReceive / I_max * 100));
        stringBuilder.AppendLine("└  " + Lang.Get("Consumption") + powerReceive + "/" + I_max + " Eu");
        //stringBuilder.AppendLine("└ " + "КПД " + this.kpd*100F + "%/" + this.kpd_max*100+"%");
        //stringBuilder.AppendLine("└ " + "Реальный ток " + I_value + "/" + this.I_max);
        stringBuilder.AppendLine();

    }
}
