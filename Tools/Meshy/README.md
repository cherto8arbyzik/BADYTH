# Meshy MCP for Codex

## Installed configuration

The official `@meshy-ai/meshy-mcp-server` is registered globally in:

`C:\Users\artem\.codex\config.toml`

The server is launched by Codex through `npx` and reads authentication from the Windows user environment variable `MESHY_API_KEY`. The API key is intentionally not stored in this repository or in `config.toml`.

## One-time key setup

1. Open <https://www.meshy.ai/settings/api> and create/copy a Meshy API key.
2. Run `D:\badyth\Tools\Meshy\setup_meshy_key.ps1` with PowerShell.
3. Paste the key into the masked prompt and press Enter.
4. Close every Codex window and reopen Codex so the MCP process inherits the new environment variable.
5. In a new Codex task ask: `Проверь баланс Meshy и перечисли доступные Meshy MCP tools`.

If Windows blocks direct script launch, run this from PowerShell:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File "D:\badyth\Tools\Meshy\setup_meshy_key.ps1"
```

## Recommended Unity character workflow

1. Use `meshy_multi_image_to_3d` with the five locked views and request FBX output.
2. Confirm the displayed credit cost before submitting generation.
3. Inspect the generated result before spending credits on refinement.
4. Use Meshy remesh with quad topology and a 50–70k target for LOD0.
5. Download FBX and textures into the project's `art/characters/...` source directory.
6. Finish exact silhouette, accessory count, scale, pivot, materials, LODs, and collision in Blender/Unity.

Meshy generation and post-processing calls consume account credits. Codex must show the cost and ask for confirmation before each paid call.
