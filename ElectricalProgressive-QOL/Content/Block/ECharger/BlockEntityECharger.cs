using System.Collections.Generic;
using System.Linq;
using System.Text;
using Cairo;
using ElectricalProgressive.Interface;
using ElectricalProgressive.Utils;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Facing = ElectricalProgressive.Utils.Facing;

namespace ElectricalProgressive.Content.Block.ECharger;

public class BlockEntityECharger : BlockEntity, ITexPositionSource
{
    public InventoryGeneric inventory;
    private int consume;
    MeshData[] toolMeshes = new MeshData[1];

    public Size2i AtlasSize => ((ICoreClientAPI)Api).BlockTextureAtlas.Size;

    CollectibleObject tmpItem;



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




    public TextureAtlasPosition this[string textureCode]
    {
        get
        {
            ToolTextures tt = null;

            if (BlockECharger.ToolTextureSubIds(Api).TryGetValue((Vintagestory.API.Common.Item)tmpItem, out tt))
            {
                int textureSubId = 0;
                if (tt.TextureSubIdsByCode.TryGetValue(textureCode, out textureSubId))
                {
                    return ((ICoreClientAPI)Api).BlockTextureAtlas.Positions[textureSubId];
                }

                return ((ICoreClientAPI)Api).BlockTextureAtlas.Positions[tt.TextureSubIdsByCode.First().Value];
            }

            return null!;
        }
    }

    private BEBehaviorElectricalProgressive? ElectricalProgressive => GetBehavior<BEBehaviorElectricalProgressive>();

    public BlockEntityECharger()
    {
        inventory = new InventoryGeneric(1, "charger", null, null, null);

        consume = MyMiniLib.GetAttributeInt(this.Block, "consume", 20);
    }

    public override void Initialize(ICoreAPI api)
    {
        base.Initialize(api);
        inventory.LateInitialize("charger-" + Pos, api);
        inventory.ResolveBlocksOrItems();

        if (api is ICoreClientAPI)
        {
            loadToolMeshes();
        }
        else
        {
            RegisterGameTickListener(OnTick, 500);
        }
    }

    //проверка, нужно ли заряжать
    private void OnTick(float dt)
    {
        var stack = inventory[0]?.Itemstack;

        if (stack?.Item is IEnergyStorageItem)
        {
            int durability = stack.Attributes.GetInt("durability");             //текущая прочность
            int maxDurability = stack.Collectible.GetMaxDurability(stack);       //максимальная прочность

            if (durability < maxDurability && GetBehavior<BEBehaviorECharger>().powerSetting > 0)
            {
                if (this.Block.Variant["state"] != "enabled")     //чтобы лишний раз не обновлять модель
                {
                    Api.World.BlockAccessor.ExchangeBlock(Api.World.GetBlock(Block.CodeWithVariant("state", "enabled")).BlockId, Pos);
                    MarkDirty(true);
                }

                ((IEnergyStorageItem)stack.Item).receiveEnergy(stack, GetBehavior<BEBehaviorECharger>().powerSetting);
            }
            else
            {
                if (this.Block.Variant["state"] != "disabled")   //чтобы лишний раз не обновлять модель
                {
                    Api.World.BlockAccessor.ExchangeBlock(Api.World.GetBlock(Block.CodeWithVariant("state", "disabled")).BlockId, Pos);
                    Api.World.PlaySoundAt(new AssetLocation("electricalprogressiveqol:sounds/din_din_din"), Pos.X, Pos.Y, Pos.Z, null, false, 8.0F, 0.4F); //звоним если зарядилось таки
                    MarkDirty(true);
                }
            }
        }
        else if (stack?.Block is IEnergyStorageItem)
        {
            int durability = stack.Attributes.GetInt("durability");             //текущая прочность
            int maxDurability = stack.Collectible.GetMaxDurability(stack);       //максимальная прочность

            if (durability < maxDurability && GetBehavior<BEBehaviorECharger>().powerSetting > 0)
            {
                if (this.Block.Variant["state"] != "enabled")     //чтобы лишний раз не обновлять модель
                {
                    Api.World.BlockAccessor.ExchangeBlock(Api.World.GetBlock(Block.CodeWithVariant("state", "enabled")).BlockId, Pos);
                    MarkDirty(true);
                }

                ((IEnergyStorageItem)stack.Item).receiveEnergy(stack, GetBehavior<BEBehaviorECharger>().powerSetting);
            }
            else
            {
                if (this.Block.Variant["state"] != "disabled")   //чтобы лишний раз не обновлять модель
                {
                    Api.World.BlockAccessor.ExchangeBlock(Api.World.GetBlock(Block.CodeWithVariant("state", "disabled")).BlockId, Pos);
                    Api.World.PlaySoundAt(new AssetLocation("electricalprogressiveqol:sounds/din_din_din"), Pos.X, Pos.Y, Pos.Z, null, false, 8.0F, 0.4F); //звоним если зарядилось таки
                    MarkDirty(true);
                }
            }
        }

        MarkDirty();
    }


    /// <summary>
    /// Загружает меши инструментов 
    /// </summary>
    void loadToolMeshes()
    {
        toolMeshes[0] = null; //должна быть сброшена сразу же 

        IItemStack stack = inventory[0].Itemstack;
        if (stack == null) //пустой стак нам не интересен
            return;

        tmpItem = stack.Collectible;

        Vec3f origin = new Vec3f(0.5f, 0.5f, 0.5f);
        ICoreClientAPI clientApi = (ICoreClientAPI)Api;


        if (stack.Class == EnumItemClass.Item)
            clientApi.Tesselator.TesselateItem(stack.Item, out toolMeshes[0], this);
        else
            clientApi.Tesselator.TesselateBlock(stack.Block, out toolMeshes[0]);

        clientApi.TesselatorManager.ThreadDispose(); //обязательно


        if (stack.Class == EnumItemClass.Item)
        {
            float scaleX = MyMiniLib.GetAttributeFloat(stack.Item, "scaleX", 0.5F);
            float scaleY = MyMiniLib.GetAttributeFloat(stack.Item, "scaleY", 0.5F);
            float scaleZ = MyMiniLib.GetAttributeFloat(stack.Item, "scaleZ", 0.5F);
            float translateX = MyMiniLib.GetAttributeFloat(stack.Item, "translateX", 0F);
            float translateY = MyMiniLib.GetAttributeFloat(stack.Item, "translateY", 0.4F);
            float translateZ = MyMiniLib.GetAttributeFloat(stack.Item, "translateZ", 0F);
            float rotateX = MyMiniLib.GetAttributeFloat(stack.Item, "rotateX", 0F);
            float rotateY = MyMiniLib.GetAttributeFloat(stack.Item, "rotateY", 0F);
            float rotateZ = MyMiniLib.GetAttributeFloat(stack.Item, "rotateZ", 0F);


            origin.Y = 1f / 30f;
            toolMeshes[0].Scale(origin, scaleX, scaleY, scaleZ);
            toolMeshes[0].Translate(translateX, translateY, translateZ);
            toolMeshes[0].Rotate(origin, rotateX, rotateY, rotateZ);
        }
        else
        {
            toolMeshes[0].Scale(origin, 0.3f, 0.3f, 0.3f);
        }
    }

    internal bool OnPlayerInteract(IPlayer byPlayer, Vec3d hit)
    {
        if (inventory[0].Itemstack != null)
        {
            return TakeFromSlot(byPlayer, 0);
        }

        return PutInSlot(byPlayer, 0);
    }

    bool PutInSlot(IPlayer player, int slot)
    {
        IItemStack stack = player.InventoryManager.ActiveHotbarSlot.Itemstack;
        if (stack == null || !(stack.Class == EnumItemClass.Block ? stack.Block is IEnergyStorageItem : stack.Item is IEnergyStorageItem))
            return false;

        player.InventoryManager.ActiveHotbarSlot.TryPutInto(Api.World, inventory[slot]);

        didInteract(player);
        return true;
    }

    bool TakeFromSlot(IPlayer player, int slot)
    {
        ItemStack stack = inventory[slot].TakeOutWhole();

        if (!player.InventoryManager.TryGiveItemstack(stack))
        {
            Api.World.SpawnItemEntity(stack, Pos.ToVec3d().Add(0.5, 0.5, 0.5));
        }
        Api.World.BlockAccessor.ExchangeBlock(Api.World.GetBlock(Block.CodeWithVariant("state", "disabled")).BlockId, Pos);
        didInteract(player);
        return true;
    }

    void didInteract(IPlayer player)
    {
        Api.World.PlaySoundAt(new AssetLocation("sounds/player/buildhigh"), Pos.X, Pos.Y, Pos.Z, player, false);
        if (Api is ICoreClientAPI)
            loadToolMeshes();

        MarkDirty(true);
    }

    public override void OnBlockPlaced(ItemStack? byItemStack = null)
    {
        base.OnBlockPlaced(byItemStack);

        var electricity = ElectricalProgressive;

        if (electricity == null || byItemStack == null)
            return;


        electricity.Connection = Facing.DownAll;

        //задаем параметры блока/проводника
        var voltage = MyMiniLib.GetAttributeInt(byItemStack!.Block, "voltage", 32);
        var maxCurrent = MyMiniLib.GetAttributeFloat(byItemStack!.Block, "maxCurrent", 5.0F);
        var isolated = MyMiniLib.GetAttributeBool(byItemStack!.Block, "isolated", false);
        var isolatedEnvironment = MyMiniLib.GetAttributeBool(byItemStack!.Block, "isolatedEnvironment", false);

        electricity.Eparams = (
            new EParams(voltage, maxCurrent, "", 0, 1, 1, false, isolated, isolatedEnvironment),
            FacingHelper.Faces(Facing.DownAll).First().Index);

    }

    public override void OnBlockRemoved()
    {
        base.OnBlockRemoved();

    }

    public override void OnBlockBroken(IPlayer? byPlayer = null)
    {
        base.OnBlockBroken(byPlayer);
        ItemStack stack = inventory[0].Itemstack;
        if (stack != null)
            Api.World.SpawnItemEntity(stack, Pos.ToVec3d().Add(0.5, 0.5, 0.5));
    }

    public override bool OnTesselation(ITerrainMeshPool mesher, ITesselatorAPI tesselator)
    {
        ICoreClientAPI clientApi = (ICoreClientAPI)Api;
        Vintagestory.API.Common.Block block = Api.World.BlockAccessor.GetBlock(Pos);
        MeshData mesh = clientApi.TesselatorManager.GetDefaultBlockMesh(block);
        if (mesh == null)
            return true;

        mesher.AddMeshData(mesh);

        if (toolMeshes[0] != null)
            mesher.AddMeshData(toolMeshes[0]);


        return true;
    }

    public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldForResolving)
    {
        base.FromTreeAttributes(tree, worldForResolving);
        inventory.FromTreeAttributes(tree.GetTreeAttribute("inventory"));
        if (Api != null)
        {
            inventory.Api = Api;
            inventory.ResolveBlocksOrItems();
        }

        if (Api is ICoreClientAPI)
        {
            loadToolMeshes();
            Api.World.BlockAccessor.MarkBlockDirty(Pos);
        }
    }

    public override void ToTreeAttributes(ITreeAttribute tree)
    {
        base.ToTreeAttributes(tree);
        ITreeAttribute invtree = new TreeAttribute();
        inventory.ToTreeAttributes(invtree);
        tree["inventory"] = invtree;
    }

    public override void OnStoreCollectibleMappings(Dictionary<int, AssetLocation> blockIdMapping, Dictionary<int, AssetLocation> itemIdMapping)
    {
        foreach (var slot in inventory)
        {
            if (slot.Itemstack == null)
                continue;

            if (slot.Itemstack.Class == EnumItemClass.Item)
            {
                itemIdMapping[slot.Itemstack.Item.Id] = slot.Itemstack.Item.Code;
            }
            else
            {
                blockIdMapping[slot.Itemstack.Block.BlockId] = slot.Itemstack.Block.Code;
            }
        }
    }

    public override void OnLoadCollectibleMappings(IWorldAccessor worldForResolve, Dictionary<int, AssetLocation> oldBlockIdMapping, Dictionary<int, AssetLocation> oldItemIdMapping, int schematicSeed)
    {
        foreach (var slot in inventory)
        {
            if (slot.Itemstack == null)
                continue;

            if (!slot.Itemstack.FixMapping(oldBlockIdMapping, oldItemIdMapping, worldForResolve))
            {
                slot.Itemstack = null;
            }
        }
    }


    /// <summary>
    /// Информация о блоке
    /// </summary>
    /// <param name="forPlayer"></param>
    /// <param name="stringBuilder"></param>
    public override void GetBlockInfo(IPlayer forPlayer, StringBuilder stringBuilder)
    {
        base.GetBlockInfo(forPlayer, stringBuilder);

        var stack = inventory[0]?.Itemstack;
        if (stack?.Item is IEnergyStorageItem) //предмет
        {
            int energy = stack.Attributes.GetInt("durability") * consume;
            int maxEnergy = stack.Collectible.GetMaxDurability(stack) * consume;

            stringBuilder.AppendLine();
            stringBuilder.AppendLine(stack.GetName());
            stringBuilder.AppendLine(StringHelper.Progressbar(energy * 100.0F / maxEnergy));
            stringBuilder.AppendLine("└ " + Lang.Get("Storage") + ": " + energy + "/" + maxEnergy + " " + Lang.Get("J"));
        }
        else if (stack?.Block is IEnergyStorageItem) //блок
        {
            int energy = stack.Attributes.GetInt("durability") * consume;
            int maxEnergy = stack.Collectible.GetMaxDurability(stack) * consume;

            stringBuilder.AppendLine();
            stringBuilder.AppendLine(stack.GetName());
            stringBuilder.AppendLine(StringHelper.Progressbar(energy * 100.0F / maxEnergy));
            stringBuilder.AppendLine("└ " + Lang.Get("Storage") + ": " + energy + "/" + maxEnergy + " " + Lang.Get("J"));
        }
    }

}




public class ToolTextures
{
    public Dictionary<string, int> TextureSubIdsByCode = new Dictionary<string, int>();
}