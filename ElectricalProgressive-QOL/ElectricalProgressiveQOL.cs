using ElectricalProgressive.Content.Block.ECharger;
using ElectricalProgressive.Content.Block.EFreezer;
using ElectricalProgressive.Content.Block.EHorn;
using ElectricalProgressive.Content.Block.EStove;
using ElectricalProgressive.Content.Block.ELamp;
using ElectricalProgressive.Content.Block.EOven;
using Vintagestory.API.Common;
using Vintagestory.API.Client;
using ElectricalProgressive.Content.Block.EHeater;
using ElectricalProgressive.Content.Item.Tool;


[assembly: ModDependency("game", "1.20.0")]
[assembly: ModDependency("electricalprogressivecore", "0.9.0")]
[assembly: ModDependency("electricalprogressivebasics", "0.9.0")]
[assembly: ModInfo(
    "Electrical Progressive: QoL",
    "electricalprogressiveqol",
    Website = "https://github.com/tehtelev/ElectricalProgressiveQOL",
    Description = "Brings electricity into the game!",
    Version = "0.9.0",
    Authors = new[] {
        "Tehtelev",
        "Kotl"
    }
)]

namespace ElectricalProgressive;

public class ElectricalProgressiveQOL : ModSystem
{

    private ICoreAPI api = null!;
    private ICoreClientAPI capi = null!;


    public override void Start(ICoreAPI api)
    {
        base.Start(api);

        this.api = api;

        api.RegisterBlockClass("BlockEHorn", typeof(BlockEHorn));
        api.RegisterBlockEntityBehaviorClass("BEBehaviorEHorn", typeof(BEBehaviorEHorn));
        api.RegisterBlockEntityClass("BlockEntityEHorn", typeof(BlockEntityEHorn));

        api.RegisterBlockClass("BlockELamp", typeof(BlockELamp));
        api.RegisterBlockClass("BlockESmallLamp", typeof(BlockESmallLamp));

        api.RegisterBlockEntityClass("BlockEntityELamp", typeof(BlockEntityELamp));
        api.RegisterBlockEntityBehaviorClass("BEBehaviorELamp", typeof(BEBehaviorELamp));

        api.RegisterBlockClass("BlockEHeater", typeof(BlockEHeater));
        api.RegisterBlockEntityClass("BlockEntityEHeater", typeof(BlockEntityEHeater));
        api.RegisterBlockEntityBehaviorClass("BEBehaviorEHeater", typeof(BEBehaviorEHeater));

        api.RegisterBlockClass("BlockECharger", typeof(BlockECharger));
        api.RegisterBlockEntityClass("BlockEntityECharger", typeof(BlockEntityECharger));
        api.RegisterBlockEntityBehaviorClass("BEBehaviorECharger", typeof(BEBehaviorECharger));


        api.RegisterBlockClass("BlockEStove", typeof(BlockEStove));
        api.RegisterBlockEntityClass("BlockEntityEStove", typeof(BlockEntityEStove));
        api.RegisterBlockEntityBehaviorClass("BEBehaviorEStove", typeof(BEBehaviorEStove));

        api.RegisterBlockClass("BlockEFreezer", typeof(BlockEFreezer));
        api.RegisterBlockEntityClass("BlockEntityEFreezer", typeof(BlockEntityEFreezer));
        api.RegisterBlockEntityBehaviorClass("BEBehaviorEFreezer", typeof(BEBehaviorEFreezer));

        api.RegisterBlockClass("BlockEOven", typeof(BlockEOven));
        api.RegisterBlockEntityClass("BlockEntityEOven", typeof(BlockEntityEOven));
        api.RegisterBlockEntityBehaviorClass("BEBehaviorEOven", typeof(BEBehaviorEOven));

        api.RegisterItemClass("EChisel", typeof(EChisel));
        api.RegisterItemClass("EAxe", typeof(EAxe));
        api.RegisterItemClass("EDrill", typeof(EDrill));

    }


    public override void StartClientSide(ICoreClientAPI api)
    {
        base.StartClientSide(api);
        this.capi = api;
    }

}