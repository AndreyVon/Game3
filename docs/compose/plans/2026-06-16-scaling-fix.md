# Scaling Fix Plan: Unified Canvas Layout

**Goal:** Make all game elements (board, tiles, UI, background) scale correctly at any screen resolution without drifting apart.

**Root cause of current issues:** Two competing scaling systems exist simultaneously:
1. **World-space** (BoardResponsiveScaler + FitBackgroundToCamera) — scales via Camera.orthographicSize
2. **Canvas-space** (GameLayoutSetup + FitBackgroundToCanvas) — scales via CanvasScaler

Plus the tile creation code tries to support both paths, creating confusion and broken references.

**Solution:** Go fully Canvas-based. One system, one source of truth.

---

## Architecture

```
Canvas (CanvasScaler: Scale With Screen Size, 1920×1080, Match=0.5)
├── Background (Image, stretch anchors, FitBackgroundToCanvas)
├── TopPanel (RectTransform, anchored top)
│   ├── BorschtPotUI
│   ├── TimerUI
│   └── LevelGoalUI
├── BoardArea (RectTransform, center, sized by GameLayoutSetup)
│   └── Tiles (UI Image children, positioned by GameLayoutSetup.GetTilePosition)
├── ScoreBar (RectTransform, below top panel)
│   └── ScoreManager/ScoreText
└── WinLoseScreen (overlay, disabled by default)
```

**Why canvas?** For a 2D puzzle game, canvas with CanvasScaler handles all resolution scaling automatically. No manual camera math, no viewport percentages, no competing systems.

---

## Step-by-step Plan

### Step 1: Commit current state (safe baseline)

Commit all uncommitted changes so we have a known-good starting point.

**Action:** `git add -A && git commit -m "checkpoint: current state before scaling fix"`

---

### Step 2: Remove world-space scaling scripts

Delete these files (they conflict with canvas approach):
- `Assets/_Project/Scripts/BoardResponsiveScaler.cs`
- `Assets/_Project/Scripts/FitBackgroundToCamera.cs`

**Verify:** No compile errors (these scripts aren't referenced by other code except in scene).

---

### Step 3: Clean up GameLayoutSetup

Rewrite `GameLayoutSetup.cs` to be the single source of truth for layout:
- Configure CanvasScaler (Scale With Screen Size, 1920×1080, Match=0.5)
- Calculate board area size based on available space
- Provide `GetTilePosition(x, y)` and `GetCellSize()` for tile positioning
- Handle top panel, score bar, and board area positioning

**Verify:** Script compiles, GameLayoutSetup component exists in scene.

---

### Step 4: Fix TileView for canvas mode

Update `TileView.cs` to work with UI Image:
- Support both SpriteRenderer (legacy) and Image (canvas) via `GetComponent` fallback
- Create highlight/glow as child Image objects (not SpriteRenderer children)
- Keep the same public API (Select, Deselect, SetSprite, SetColor)

**Verify:** Script compiles.

---

### Step 5: Fix TileSpawner for canvas mode

Update `TileSpawner.cs`:
- `CreateCanvasTile` should create a clean UI Image tile (no SpriteRenderer from prefab)
- Add sprite from prefab's SpriteRenderer to the Image component
- Set proper size based on `layoutSetup.GetCellSize()`

**Verify:** Script compiles.

---

### Step 6: Fix BoardManager canvas path

Update `BoardManager.cs`:
- Remove the dual-path logic in `InitializeBoardSystems`
- Always use canvas path when `layoutSetup` is present
- Fix `CreateCanvasTile` to properly initialize TileView with sprite
- Remove debug key C code (or keep as separate debug toggle)

**Verify:** Script compiles.

---

### Step 7: Wire up scene hierarchy in Unity Editor

In `GameScene.unity`:
1. Create Canvas with CanvasScaler (Scale With Screen Size, 1920×1080, Match=0.5)
2. Create Background child → Image component → FitBackgroundToCanvas
3. Create TopPanel → add BorschtPotUI, TimerUI, LevelGoalUI
4. Create BoardArea → add Image (board background) → assign to GameLayoutSetup
5. Create ScoreBar → add ScoreManager/ScoreText
6. Create WinLoseScreen overlay
7. Add GameLayoutSetup to Canvas, assign all references
8. Add LevelManager, assign LevelData
9. Update BoardManager references (layoutSetup, boardArea)
10. Remove old world-space objects (Camera scaling, SpriteRenderer board)

**Verify:** Scene loads without errors, Play mode starts.

---

### Step 8: Test basic scaling

1. Press Play at 1920×1080 → board centered, UI at top, all visible
2. Resize to 1280×720 → everything scales proportionally
3. Resize to 3840×2160 → everything scales proportionally
4. Test portrait (1080×1920) → layout adapts (or at least doesn't break)

**Verify:** No elements drift apart at any resolution.

---

### Step 9: Test gameplay

1. Make matches → tiles animate correctly
2. Score updates → ScoreManager shows correct value
3. Timer counts down → TimerUI displays correctly
4. Vegetable goals update → LevelGoalUI shows progress
5. Win condition → WinLoseScreen appears with stars
6. Lose condition (timer out) → Lose screen appears
7. Retry button → level restarts

**Verify:** Full gameplay loop works.

---

### Step 10: Final cleanup and commit

1. Remove any leftover debug logs
2. Remove old world-space references from BoardManager (boardSpriteRenderer, markers, etc.)
3. Commit: `git commit -m "feat: unified canvas-based layout for proper resolution scaling"`

---

## Files Modified

| File | Action |
|------|--------|
| `BoardResponsiveScaler.cs` | Delete |
| `FitBackgroundToCamera.cs` | Delete |
| `GameLayoutSetup.cs` | Rewrite |
| `TileView.cs` | Update for canvas |
| `TileSpawner.cs` | Update canvas tile creation |
| `BoardManager.cs` | Remove dual-path, fix canvas |
| `FitBackgroundToCanvas.cs` | Keep (already works) |
| `GameScene.unity` | Restructure hierarchy |

## Execution order

Each step is independent — user verifies after each before I proceed to next.
