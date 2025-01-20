Some asset files are mutable when playing in Editor, use the following commands to ignore their changes:

```powershell
git update-index --skip-worktree "Assets/Skybox Anime/Materials/Skybox Anime.mat"
git update-index --skip-worktree "Assets/Skybox Anime/Materials/Cloud High.mat"
git update-index --skip-worktree "Assets/Skybox Anime/Materials/Cloud Low.mat"
```