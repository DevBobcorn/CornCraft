部分资源文件会在编辑器中运行时动态改变，可用如下命令忽略其变化：

```powershell
git update-index --skip-worktree "Assets/Skybox Anime/Materials/Skybox Anime.mat"
git update-index --skip-worktree "Assets/Skybox Anime/Materials/Cloud High.mat"
git update-index --skip-worktree "Assets/Skybox Anime/Materials/Cloud Low.mat"
```