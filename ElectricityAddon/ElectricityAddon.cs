﻿using ElectricityAddon.Content.Armor;
using ElectricityAddon.Content.Block.EAccumulator;
using ElectricityAddon.Content.Block.ECentrifuge;
using ElectricityAddon.Content.Block.ECharger;
using ElectricityAddon.Content.Block.EFreezer;
using ElectricityAddon.Content.Block.EGenerator;
using ElectricityAddon.Content.Block.EHorn;
using ElectricityAddon.Content.Block.EInductFurnance;
using ElectricityAddon.Content.Block.EMotor;
using ElectricityAddon.Content.Block.EPress;
using ElectricityAddon.Content.Block.EStove;
using ElectricityAddon.Content.Item;
using Vintagestory.API.Common;

[assembly: ModDependency("game", "1.19.5")]
[assembly: ModDependency("electricity", "0.0.11")]
[assembly: ModInfo(
    "ElectricityAddon",
    "electricityaddon",
    Website = "https://github.com/Kotl-EV/ElectricityAddon",
    Description = "Brings electricity into the game!",
    Version = "0.0.8",
    Authors = new[] {
        "Kotl"
    }
)]

namespace ElectricityAddon;

public class ElectricityAddon : ModSystem
{
    public override void Start(ICoreAPI api)
    {
        base.Start(api);
        
        api.RegisterBlockClass("BlockEHorn", typeof(BlockEHorn));
        api.RegisterBlockEntityBehaviorClass("BEBehaviorEHorn", typeof(BEBehaviorEHorn));
        api.RegisterBlockEntityClass("BlockEntityEHorn", typeof(BlockEntityEHorn));

        api.RegisterBlockClass("BlockEAccumulator", typeof(BlockEAccumulator));
        api.RegisterBlockEntityBehaviorClass("BEBehaviorEAccumulator", typeof(BEBehaviorEAccumulator));

        api.RegisterBlockClass("BlockECharger", typeof(BlockECharger));
        api.RegisterBlockEntityClass("BlockEntityECharger", typeof(BlockEntityECharger));
        api.RegisterBlockEntityBehaviorClass("BEBehaviorECharger", typeof(BEBehaviorECharger));

        api.RegisterBlockClass("BlockEStove", typeof(BlockEStove));
        api.RegisterBlockEntityClass("BlockEntityEStove", typeof(BlockEntityEStove));
        api.RegisterBlockEntityBehaviorClass("BEBehaviorEStove", typeof(BEBehaviorEStove));
        
        api.RegisterBlockClass("BlockEInductFurnance", typeof(BlockEInductFurnance));
        api.RegisterBlockEntityClass("BlockEntityEInductFurnance", typeof(BlockEntityEInductFurnance));
        api.RegisterBlockEntityBehaviorClass("BEBehaviorEInductFurnance", typeof(BEBehaviorEInductFurnance));

        api.RegisterBlockClass("BlockEFreezer", typeof(BlockEFreezer));
        api.RegisterBlockEntityClass("BlockEntityEFreezer", typeof(BlockEntityEFreezer));
        api.RegisterBlockEntityBehaviorClass("BEBehaviorEFreezer", typeof(BEBehaviorEFreezer));
        
        api.RegisterBlockClass("BlockECentrifuge", typeof(BlockECentrifuge));
        api.RegisterBlockEntityClass("BlockEntityECentrifuge", typeof(BlockEntityECentrifuge));
        api.RegisterBlockEntityBehaviorClass("BEBehaviorECentrifuge", typeof(BEBehaviorECentrifuge));
        
        api.RegisterBlockClass("BlockEPress", typeof(BlockEPress));
        api.RegisterBlockEntityClass("BlockEntityEPress", typeof(BlockEntityEPress));
        api.RegisterBlockEntityBehaviorClass("BEBehaviorEPress", typeof(BEBehaviorEPress));
        
        api.RegisterBlockClass("BlockEMotorTier1", typeof(BlockEMotorTier1));
        api.RegisterBlockClass("BlockEMotorTier2", typeof(BlockEMotorTier2));
        api.RegisterBlockClass("BlockEMotorTier3", typeof(BlockEMotorTier3));
        api.RegisterBlockEntityClass("BlockEntityEMotor", typeof(BlockEntityEMotor));
        api.RegisterBlockEntityBehaviorClass("BEBehaviorEMotorTier1", typeof(BEBehaviorEMotorTier1));
        api.RegisterBlockEntityBehaviorClass("BEBehaviorEMotorTier2", typeof(BEBehaviorEMotorTier2));
        api.RegisterBlockEntityBehaviorClass("BEBehaviorEMotorTier3", typeof(BEBehaviorEMotorTier3));
        
        api.RegisterBlockClass("BlockEGeneratorTier1", typeof(BlockEGeneratorTier1));
        api.RegisterBlockClass("BlockEGeneratorTier2", typeof(BlockEGeneratorTier2));
        api.RegisterBlockClass("BlockEGeneratorTier3", typeof(BlockEGeneratorTier3));
        api.RegisterBlockEntityClass("BlockEntityEGenerator", typeof(BlockEntityEGenerator));
        api.RegisterBlockEntityBehaviorClass("BEBehaviorEGeneratorTier1", typeof(BEBehaviorEGeneratorTier1));
        api.RegisterBlockEntityBehaviorClass("BEBehaviorEGeneratorTier2", typeof(BEBehaviorEGeneratorTier2));
        api.RegisterBlockEntityBehaviorClass("BEBehaviorEGeneratorTier3", typeof(BEBehaviorEGeneratorTier3));

        api.RegisterItemClass("EChisel", typeof(EChisel));
        api.RegisterItemClass("EAxe", typeof(EAxe));
        api.RegisterItemClass("EDrill", typeof(EDrill));
        api.RegisterItemClass("EArmor", typeof(EArmor));
        api.RegisterItemClass("EWeapon", typeof(EWeapon));
        api.RegisterItemClass("EShield", typeof(EShield));
    }
}