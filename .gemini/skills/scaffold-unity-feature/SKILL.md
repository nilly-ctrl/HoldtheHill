---
name: scaffold-unity-feature
description: Scaffolds a new feature directory structure under Assets/_Game/Features/<FeatureName>/ according to Hold the Hill README standards.
---

# Scaffold Unity Feature Skill

Use this skill when starting a new gameplay feature for **Hold the Hill**.

## Workflow

1. Specify the `<FeatureName>` (PascalCase, e.g., `Towers`, `Enemies`, `Combat`).
2. Create the directory tree under `unity/Assets/_Game/Features/<FeatureName>/`:
   ```
   unity/Assets/_Game/Features/<FeatureName>/
     Scripts/
     Prefabs/
     Data/
     Art/
     Animations/
     Audio/
   ```
3. Ensure initial C# scripts created inside `Scripts/` use the matching namespace:
   ```csharp
   namespace HoldTheHill.Features.<FeatureName>
   {
       public class <ClassName>
       {
       }
   }
   ```
4. Verify `HoldTheHill.Runtime.asmdef` covers the new directory.
