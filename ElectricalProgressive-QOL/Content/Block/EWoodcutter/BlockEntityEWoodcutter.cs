using ElectricalProgressive.Utils;
using System;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

namespace ElectricalProgressive.Content.Block.EWoodcutter;

public class BlockEntityEWoodcutter : BlockEntityOpenableContainer
{
    private ICoreClientAPI? _clientApi;
    private ICoreServerAPI? _serverApi;
    private BlockPos _startTreePos;
    private Stack<BlockPos>? _allTreePos;

    public bool IsNotEnoughEnergy { get; set; }
    public int WoodTier { get; private set; }
    public int TreeResistance { get; private set; }
    public WoodcutterStage Stage { get; private set; }


    #region ElectricalProgressive

    protected BEBehaviorElectricalProgressive? ElectricalProgressive => GetBehavior<BEBehaviorElectricalProgressive>();

    public (EParams, int) Eparams
    {
        get => ElectricalProgressive?.Eparams ?? (new(), 0);
        set => ElectricalProgressive!.Eparams = value;
    }

    public EParams[] AllEparams
    {
        get => ElectricalProgressive?.AllEparams ?? new EParams[]
        {
            new(),
            new(),
            new(),
            new(),
            new(),
            new()
        };
        set
        {
            if (ElectricalProgressive != null)
                ElectricalProgressive.AllEparams = value;
        }
    }

    public const string AllEparamsKey = "electricalprogressive:allEparams";

    #endregion

    #region Inventory
    public override string InventoryClassName => "InvEWoodcutter";


    private InventoryEWoodcutter _inventory;

    /// <summary>
    /// SlotID: 0 = input, 1-5 = output
    /// </summary>
    public override InventoryBase Inventory => _inventory;

    public bool HasSeed => !_inventory[0].Empty;

    #endregion

    public BlockEntityEWoodcutter()
    {
        _inventory = new(null);
    }

    public override void Initialize(ICoreAPI api)
    {
        base.Initialize(api);

        _startTreePos = Pos.UpCopy();

        if (api.Side.IsClient())
            _clientApi = api as ICoreClientAPI;

        if (Api.Side.IsServer())
            _serverApi = api as ICoreServerAPI;

        _inventory.Pos = Pos;
        _inventory.LateInitialize($"{InventoryClassName}-{Pos.X}/{Pos.Y}/{Pos.Z}", api);

        RegisterGameTickListener(StageWatcher, 300);
        RegisterGameTickListener(ChopTreeUpdate, 500);
    }

    /// <summary>
    /// Отслеживает состояние и переходит в нужные стадии
    /// </summary>
    private void StageWatcher(float dt)
    {
        if (Stage == WoodcutterStage.PlantTree)
        {
            Stage = WoodcutterStage.WaitFullGrowth;
            PlantSapling();
            return;
        }

        var currentTreeBlock = Api.World.BlockAccessor.GetBlock(_startTreePos);
        if (currentTreeBlock.Id == 0)
        {
            Stage = HasSeed
                ? WoodcutterStage.PlantTree
                : WoodcutterStage.None;
            return;
        }

        var canShop = CanBreakBlock(currentTreeBlock);
        if (canShop)
        {
            Stage = WoodcutterStage.ChopTree;
            return;
        }

        // Если это плохой саженец, то ломаем
        if (currentTreeBlock is not BlockSapling blockSapling)
            return;

        var isAllowedForGrow = blockSapling.Variant["wood"] switch
        {
            "redwood" => false,

            _ => true
        };
        if (isAllowedForGrow)
            return;

        Api.World.BlockAccessor.BreakBlock(_startTreePos, null);
        MarkDirty();
    }

    private void PlantSapling()
    {
        if (IsNotEnoughEnergy)
            return;

        if (_inventory[0]?.Itemstack?.Collectible is not ItemTreeSeed treeSeed)
            return;

        var treeType = treeSeed.Variant["type"];

        var saplingBlock = Api.World.GetBlock(AssetLocation.Create("sapling-" + treeType + "-free"));
        if (saplingBlock is null)
            return;

        Api.World.BlockAccessor.SetBlock(
            saplingBlock.Id,
            _startTreePos,
            _inventory[0].Itemstack
        );
        Api.World.PlaySoundAt(saplingBlock.Sounds.Place, _startTreePos.X, _startTreePos.Y, _startTreePos.Z);

        _inventory[0].TakeOut(1);
        _inventory[0].MarkDirty();

        MarkDirty();
    }

    private int _blocksBroken;
    private float _leavesMul;
    private float _leavesBranchyMul;

    private void ChopTreeUpdate(float dt)
    {
        if (Stage != WoodcutterStage.ChopTree)
            return;

        if (IsNotEnoughEnergy)
            return;

        // Если обработали все дерево, то выходим
        if (_allTreePos is not null && _allTreePos.Count == 0)
        {
            Stage = WoodcutterStage.None;
            TreeResistance = WoodTier = 0;
            _blocksBroken = 0;
            _leavesMul = 1;
            _leavesBranchyMul = 0.8f;
            _allTreePos = null;
            MarkDirty();
            return;
        }

        if (_allTreePos is null)
        {
            _allTreePos = FindTree(Api.World, _startTreePos, out int resistance, out int woodTier);
            TreeResistance = resistance;
            WoodTier = woodTier;

            // Дерева нет, но есть 1 блок
            if (Api.Side.IsServer() && _allTreePos.Count == 0)
            {
                var oneTreeBlock = Api.World.BlockAccessor.GetBlock(_startTreePos);
                BreakBlockAndCollect(oneTreeBlock, _startTreePos, 1f);

                return;
            }
        }

        if (Api.Side.IsClient())
            return;

        if (!_allTreePos.TryPop(out var pos))
            return;

        var block = Api.World.BlockAccessor.GetBlock(pos);
        if (block.BlockMaterial == EnumBlockMaterial.Air)
            return;

        _blocksBroken++;
        var isBranchy = block.Code.Path.Contains("branchy");
        var isLeaves = block.BlockMaterial == EnumBlockMaterial.Leaves;

        var dropQuantityMultiplier = isLeaves
            ? _leavesMul
            : isBranchy
                ? _leavesBranchyMul
                : 1;
        BreakBlockAndCollect(block, pos, dropQuantityMultiplier);

        if (isLeaves && _leavesMul > 0.03f)
            _leavesMul *= 0.85f;

        if (isBranchy && _leavesBranchyMul > 0.015f)
            _leavesBranchyMul *= 0.7f;
    }

    private void BreakBlockAndCollect(Vintagestory.API.Common.Block block, BlockPos pos, float dropQuantityMultiplier = 1f)
    {
        var drops = block.GetDrops(Api.World, pos, null, dropQuantityMultiplier);
        if (drops is null)
            return;

        foreach (var stack in drops)
        {
            if (stack is null)
                continue;

            var itemPlaced = TryPutStackToInventory(stack);

            if (!itemPlaced && stack.StackSize > 0)
                Api.World.SpawnItemEntity(stack, pos.ToVec3d());
        }

        Api.World.BlockAccessor.SetBlock(0, pos);
        Api.World.PlaySoundAt(block.Sounds.Break, pos, 0, null, false, 16);

        MarkDirty();
    }

    private bool TryPutStackToInventory(ItemStack stack)
    {
        var itemPlaced = false;

        var isSeed = stack.Collectible.Code.Path.Contains("seed");
        // Семена помещаем только в 1 слот
        var startSlot = isSeed
            ? 0
            : 1;

        for (var i = startSlot; i < _inventory.Count; i++)
        {
            if (isSeed && i != 0)
                continue;

            var slotStack = _inventory[i].Itemstack;

            if (slotStack == null || slotStack.StackSize == 0)
            {
                _inventory[i].Itemstack = stack.Clone();
                itemPlaced = true;
                break;
            }

            var isCollectableEquals = slotStack.Collectible.Equals(
                slotStack,
                stack,
                GlobalConstants.IgnoredStackAttributes);
            if (!isCollectableEquals)
                continue;

            int moveAmount = Math.Min(
                stack.StackSize,
                slotStack.Collectible.MaxStackSize - slotStack.StackSize
            );

            slotStack.StackSize += moveAmount;
            stack.StackSize -= moveAmount;

            if (stack.StackSize <= 0)
            {
                itemPlaced = true;
                break;
            }
        }

        return itemPlaced;
    }

    private bool CanBreakBlock(Vintagestory.API.Common.Block? block) => block switch
    {
        BlockLog => true,
        BlockLogSection => true,
        BlockSapling => false,

        _ => false
    };


    public override bool OnPlayerRightClick(IPlayer byPlayer, BlockSelection blockSel)
    {
        if (Api.Side.IsClient() && _clientApi is not null)
        {
            toggleInventoryDialogClient(byPlayer, delegate
            {
                // TODO: Перевод для заголовка диалога
                invDialog = new GuiBlockEntityEWoodcutter("Инвентарь лесоруба", _inventory, Pos, _clientApi);
                return invDialog;
            });
        }

        return true;
    }

    public override void OnBlockPlaced(ItemStack? byItemStack = null)
    {
        base.OnBlockPlaced(byItemStack);

        if (ElectricalProgressive != null)
            ElectricalProgressive.Connection = Facing.DownAll;
    }

    public override void OnBlockRemoved()
    {
        base.OnBlockRemoved();

        if (invDialog is not null)
        {
            invDialog.TryClose();
            invDialog.Dispose();
            invDialog = null;
        }

        if (ElectricalProgressive != null)
            ElectricalProgressive.Connection = Facing.None;
    }

    /// <summary>
    /// Сохраняет атрибуты
    /// </summary>
    /// <param name="tree"></param>
    public override void ToTreeAttributes(ITreeAttribute tree)
    {
        base.ToTreeAttributes(tree);

        var inventoryTree = new TreeAttribute();
        Inventory.ToTreeAttributes(inventoryTree);
        tree["inventory"] = inventoryTree;

        tree.SetInt("stage", (int)Stage);
    }


    /// <summary>
    /// Загружает атрибуты 
    /// </summary>
    /// <param name="tree"></param>
    /// <param name="worldForResolving"></param>
    public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldForResolving)
    {
        base.FromTreeAttributes(tree, worldForResolving);

        Stage = (WoodcutterStage)tree.GetInt("stage", 0);

        _inventory.FromTreeAttributes(tree.GetTreeAttribute("inventory"));

        if (Api is null)
            return;

        Inventory.AfterBlocksLoaded(Api.World);

        if (Api.Side.IsClient() && invDialog is GuiBlockEntityEWoodcutter guiBlockEntityEWoodcutter)
        {
            guiBlockEntityEWoodcutter.Update();
            MarkDirty(true);
        }
    }

    #region AxeCode

    const int LeafGroups = 7;

    /// <summary>
    /// Resistance is based on 1 for leaves, 2 for branchy leaves, and 4-8 for logs depending on woodTier.
    /// WoodTier is 3 for softwoods (Janka hardness up to about 1000), 4 for temperate hardwoods (Janka hardness 1000-2000), 5 for tropical hardwoods (Janka hardness 2000-3000), and 6 for ebony (Janka hardness over 3000)
    /// </summary>
    /// <param name="world"></param>
    /// <param name="startPos"></param>
    /// <param name="resistance"></param>
    /// <param name="woodTier"></param>
    /// <returns></returns>
    public Stack<BlockPos> FindTree(IWorldAccessor world, BlockPos startPos, out int resistance, out int woodTier)
    {
        var queue = new Queue<Vec4i>();
        var leafqueue = new Queue<Vec4i>();
        var checkedPositions = new HashSet<BlockPos>();
        var foundPositions = new Stack<BlockPos>();
        resistance = 0;
        woodTier = 0;

        var block = world.BlockAccessor.GetBlock(startPos);
        if (block.Code == null)
            return foundPositions;

        var treeFellingGroupCode = block.Attributes?["treeFellingGroupCode"].AsString();
        var spreadIndex = block.Attributes?["treeFellingGroupSpreadIndex"].AsInt(0) ?? 0;
        if (block.Attributes?["treeFellingCanChop"].AsBool(true) == false)
            return foundPositions;

        var bh = EnumTreeFellingBehavior.Chop;

        if (block is ICustomTreeFellingBehavior ctfbh)
        {
            bh = ctfbh.GetTreeFellingBehavior(startPos, null, spreadIndex);
            if (bh == EnumTreeFellingBehavior.NoChop)
            {
                resistance = foundPositions.Count;
                return foundPositions;
            }
        }

        // Must start with a log
        if (spreadIndex < 2)
            return foundPositions;

        if (treeFellingGroupCode == null)
            return foundPositions;

        queue.Enqueue(new(startPos, spreadIndex));
        checkedPositions.Add(startPos);
        var adjacentLeafGroupsCounts = new int[LeafGroups];

        while (queue.Count > 0)
        {
            var pos = queue.Dequeue();
            foundPositions.Push(new(pos.X, pos.Y, pos.Z));   // dimension-correct because pos.Y contains the dimension
            resistance += pos.W + 1;      // leaves -> 1; branchyleaves -> 2; softwood -> 4 etc.
            if (woodTier == 0)
                woodTier = pos.W;

            if (foundPositions.Count > 2500)
                break;

            block = world.BlockAccessor.GetBlockRaw(pos.X, pos.Y, pos.Z, BlockLayersAccess.Solid);
            if (block is ICustomTreeFellingBehavior ctfbhh)
                bh = ctfbhh.GetTreeFellingBehavior(startPos, null, spreadIndex);

            if (bh == EnumTreeFellingBehavior.NoChop)
                continue;

            onTreeBlock(pos, world.BlockAccessor, checkedPositions, startPos, bh == EnumTreeFellingBehavior.ChopSpreadVertical, treeFellingGroupCode, queue, leafqueue, adjacentLeafGroupsCounts);
        }

        // Find which is the most prevalent of the 7 possible adjacentLeafGroups
        int maxCount = 0;
        int maxI = -1;
        for (int i = 0; i < adjacentLeafGroupsCounts.Length; i++)
        {
            if (adjacentLeafGroupsCounts[i] > maxCount)
            {
                maxCount = adjacentLeafGroupsCounts[i];
                maxI = i;
            }
        }

        // If we found adjacentleaves using the leafgroup system, update the treeFellingGroupCode for the leaves search, using the most commonly found group
        // The purpose of this is to avoid chopping the "wrong" leaf in those cases where trees are growing close together and one of tree 2's leaves is the first leaf found when chopping tree 1
        if (maxI >= 0)
            treeFellingGroupCode = (maxI + 1) + treeFellingGroupCode;

        while (leafqueue.Count > 0)
        {
            var pos = leafqueue.Dequeue();
            foundPositions.Push(new(pos.X, pos.Y, pos.Z));   // dimension-correct because pos.Y contains the dimension
            resistance += pos.W + 1;      // leaves -> 1; branchyleaves -> 2; softwood -> 4 etc.
            if (foundPositions.Count > 2500)
                break;

            onTreeBlock(pos, world.BlockAccessor, checkedPositions, startPos, bh == EnumTreeFellingBehavior.ChopSpreadVertical, treeFellingGroupCode, leafqueue, null, null);
        }

        return foundPositions;
    }

    private void onTreeBlock(Vec4i pos, IBlockAccessor blockAccessor, HashSet<BlockPos> checkedPositions, BlockPos startPos, bool chopSpreadVertical, string treeFellingGroupCode, Queue<Vec4i> queue, Queue<Vec4i> leafqueue, int[] adjacentLeaves)
    {
        Queue<Vec4i> outqueue;
        for (var i = 0; i < Vec3i.DirectAndIndirectNeighbours.Length; i++)
        {
            var facing = Vec3i.DirectAndIndirectNeighbours[i];
            var neibPos = new BlockPos(pos.X + facing.X, pos.Y + facing.Y, pos.Z + facing.Z);

            var hordist = GameMath.Sqrt(neibPos.HorDistanceSqTo(startPos.X, startPos.Z));
            var vertdist = (neibPos.Y - startPos.Y);

            // "only breaks blocks inside an upside down square base pyramid"
            var f = chopSpreadVertical ? 0.5f : 2;
            if (hordist - 1 >= f * vertdist)
                continue;
            if (checkedPositions.Contains(neibPos))
                continue;

            var block = blockAccessor.GetBlock(neibPos, BlockLayersAccess.Solid);
            if (block.Code == null || block.Id == 0)
                continue;   // Skip air blocks

            string ngcode = block.Attributes?["treeFellingGroupCode"].AsString();

            // Only break the same type tree blocks
            if (ngcode != treeFellingGroupCode)
            {
                if (ngcode == null || leafqueue == null)
                    continue;

                // Leaves now can carry treeSubType value of 1-7 therefore do a separate check for the leaves
                if (block.BlockMaterial == EnumBlockMaterial.Leaves && ngcode.Length == treeFellingGroupCode.Length + 1 && ngcode.EndsWithOrdinal(treeFellingGroupCode))
                {
                    outqueue = leafqueue;
                    int leafGroup = GameMath.Clamp(ngcode[0] - '0', 1, 7);
                    adjacentLeaves[leafGroup - 1]++;
                }
                else
                    continue;
            }
            else
                outqueue = queue;

            // Only spread from "high to low". i.e. spread from log to leaves, but not from leaves to logs
            int nspreadIndex = block.Attributes?["treeFellingGroupSpreadIndex"].AsInt(0) ?? 0;
            if (pos.W < nspreadIndex)
                continue;

            checkedPositions.Add(neibPos);

            if (chopSpreadVertical && !facing.Equals(0, 1, 0) && nspreadIndex > 0)
                continue;

            outqueue.Enqueue(new(neibPos, nspreadIndex));
        }
    }

    #endregion

    public enum WoodcutterStage
    {
        None = 0,

        PlantTree,

        WaitFullGrowth,

        ChopTree
    }
}