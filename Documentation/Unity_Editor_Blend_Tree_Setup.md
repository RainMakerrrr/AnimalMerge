# Unity Editor: Blend Tree Setup for Smooth Turning Animation

## Overview
This document provides step-by-step instructions for configuring the Cougar Animator Controller with a 1D Blend Tree for smooth turning animations. This setup enables smooth transitions between left, forward, and right walk animations.

## Prerequisites
- Unity Editor opened
- Cougar prefab or scene with Cougar animator
- Animation clips available:
  - `Coug_Walk_Left` (walking while turning left)
  - `Coug_Walk` or `Cougar_Walk` (walking straight)
  - `Coug_Walk_Right` (walking while turning right)

## Step-by-Step Instructions

### Step 1: Open the Animator Controller
1. Navigate to `Assets/Malbers Animations/Animals Packs/01 Forest Pack/Cougar/Anims/`
2. Double-click on `Cougar Animations Demo.controller`
3. The Animator window should open

### Step 2: Add TurnDirection Parameter
1. In the Animator window, locate the **Parameters** tab (usually on the left)
2. Click the **+** button
3. Select **Float**
4. Name it: `TurnDirection`
5. Set Default value: `0`
6. (Optional) If Unity supports it, set Range: `-1` to `1`

### Step 3: Create the Movement Blend Tree State

#### Option A: Creating New State
1. In the Animator window, locate the **Base Layer**
2. Right-click in the graph area
3. Select **Create State** → **From New Blend Tree**
4. Rename the new state to `Movement`

#### Option B: Converting Existing State
If you already have a `Walk` or `Movement` state:
1. Right-click on the existing state
2. Delete it (we'll recreate it as Blend Tree)
3. Follow Option A to create new Blend Tree state

### Step 4: Configure the Blend Tree
1. Double-click on the `Movement` state to enter the Blend Tree
2. Select the Blend Tree node in the graph
3. In the **Inspector** window, configure:
   - **Blend Type**: `1D`
   - **Parameter**: `TurnDirection`

### Step 5: Add Animation Clips to Blend Tree
1. In the Inspector, find the **Motion** section
2. Click the **+** button 3 times to add 3 motion fields
3. Configure each motion:

   **Motion 1 (Left Turn):**
   - Threshold: `-1.0`
   - Motion: `Coug_Walk_Left`

   **Motion 2 (Straight):**
   - Threshold: `0.0`
   - Motion: `Coug_Walk` (or `Cougar_Walk` if that's the name)

   **Motion 3 (Right Turn):**
   - Threshold: `1.0`
   - Motion: `Coug_Walk_Right`

4. Verify the red preview bar shows smooth blending between animations

### Step 6: Configure Transitions

#### Idle → Movement Transition
1. Go back to Base Layer (click "Base Layer" in breadcrumbs)
2. Right-click on `Idle` state
3. Select **Make Transition**
4. Click on `Movement` state
5. Select the transition arrow
6. In Inspector, configure:
   - **Conditions**: Click **+** → Add condition: `MoveSpeed` `Greater` `0.1`
   - **Transition Duration**: `0.2` (or 0.15-0.25 for preference)
   - **Exit Time**: Uncheck "Has Exit Time" (for immediate response)

#### Movement → Idle Transition
1. Right-click on `Movement` state
2. Select **Make Transition**
3. Click on `Idle` state
4. Select the transition arrow
5. In Inspector, configure:
   - **Conditions**: Click **+** → Add condition: `MoveSpeed` `Less` `0.1`
   - **Transition Duration**: `0.2`
   - **Exit Time**: Uncheck "Has Exit Time"

### Step 7: Fine-Tuning (Optional)

#### Adjust Transition Smoothness
- **Slower, smoother**: Increase Transition Duration to `0.3-0.4`
- **Faster, snappier**: Decrease Transition Duration to `0.1-0.15`

#### Adjust Blend Tree Thresholds
If you want more/less aggressive turning animations:
- Make turns more subtle: Change thresholds to `-0.5`, `0`, `0.5`
- Make turns more pronounced: Keep at `-1`, `0`, `1`

### Step 8: Test in Play Mode
1. Save the Animator Controller (Ctrl+S)
2. Enter Play Mode
3. Observe the Animator window while testing:
   - Watch the `TurnDirection` parameter value
   - Verify smooth blending in the Blend Tree visualization
4. Test scenarios:
   - Walk straight → should play `Coug_Walk`
   - Turn left during movement → should blend to `Coug_Walk_Left`
   - Turn right during movement → should blend to `Coug_Walk_Right`
   - Sharp 180° turn → should show strong turn animation

## Troubleshooting

### Problem: TurnDirection parameter not found
**Solution**: Make sure you created the Float parameter named exactly `TurnDirection` (case-sensitive)

### Problem: Animations don't blend smoothly
**Solution**:
- Check that all 3 animations have similar speeds
- Increase Transition Duration
- Verify Motion clips are assigned correctly

### Problem: Character keeps turning animation even when stopped
**Solution**:
- Check that `UpdateTurnDirection(0f)` is called in code when movement stops
- Verify the `Movement → Idle` transition condition is correct

### Problem: Blend Tree doesn't show in Animator
**Solution**:
- Make sure you double-clicked the Blend Tree state to enter it
- Check that the Blend Tree node is selected in the graph

### Problem: Animation "pops" or "jumps" during transitions
**Solution**:
- Increase Transition Duration to 0.3-0.4
- Enable "Blend Tree" preview in Animator to visualize

## Code Integration Verification

After completing the Unity Editor setup, verify the code integration:

1. **AnimalAnimator.cs** should have:
   - `TurnDirection` parameter constant
   - `UpdateTurnDirection(float)` method
   - Initialization in `Awake()`

2. **AnimalMovement.cs** should have:
   - `_rotationSpeed` serialized field
   - `_targetRotation` private field
   - `Update()` method with Slerp rotation
   - `UpdateTurnDirectionAnimation()` method
   - Reset `TurnDirection` to 0 in `ExecuteMovement`

## Applying to Other Animals (Future)

To apply this system to other animals (Chicken, Fox, Hedgehog):

1. Check if the animal has Left/Right walk animations
2. If yes: Repeat Steps 1-7 for that animal's Animator Controller
3. If no: You can use Unity's **Mirror** feature:
   - In Blend Tree, check "Mirror" checkbox for one direction
   - This will flip the animation automatically

### Note for Chicken
Chicken has a two-layer system (Wing Layer). You may need to:
- Apply Blend Tree only to Base Layer
- Keep Wing Layer separate
- Test thoroughly to ensure layers don't conflict

## Performance Considerations

- **1D Blend Tree** is very performant (minimal overhead)
- **Slerp rotation** in Update is also lightweight
- No significant performance impact expected
- For mobile: Test on target device to verify 30-60 FPS maintained

## Expected Results

After proper setup, you should see:
- **Smooth left/right turns** during movement
- **No snapping** between animations
- **Responsive** direction changes
- **Natural-looking** animal movement
- **Parameter value** in Animator reflects turn direction

## Additional Resources

- Unity Manual: [Blend Trees](https://docs.unity3d.com/Manual/class-BlendTree.html)
- Unity Manual: [Animation Parameters](https://docs.unity3d.com/Manual/AnimationParameters.html)
- Unity Manual: [State Machine Transitions](https://docs.unity3d.com/Manual/class-Transition.html)

---

**Document Version**: 1.0
**Last Updated**: 2026-02-24
**Related Files**:
- `Assets/Code/Animals/AnimalAnimator.cs`
- `Assets/Code/Animals/Movement/AnimalMovement.cs`
- `Assets/Code/Animals/Movement/IMovementAnimator.cs`
- `Assets/Malbers Animations/.../Cougar Animations Demo.controller`
