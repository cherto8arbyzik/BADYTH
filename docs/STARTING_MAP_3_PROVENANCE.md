# Starting Map 3.0 — provenance

The source archive is stored at `landscape/стартовая карта 3.0.zip`. The FBX and its four PBR textures were copied into Unity without changing their source pixels or geometry.

## Source hashes

- Archive SHA-256: `7558558B3C1F3A6F44492BEB9CB8F7DB984DBC1D427E16590614978153619566`
- FBX SHA-256: `0CF94442A73218EFCBC7446D3F86B590F83CA07C731DC6161CCE6514E101919C`
- Base color SHA-256: `8B835B2F2231C9EB2FF274691B7477EA6045FC3A0050754CFB166255ABEF4712`
- Metallic SHA-256: `72000BE3A139DA59139E56E86D072A296587ABE44E70F6787AFC2BDD2FABBA47`
- Normal SHA-256: `BC3EB3603F112D6F66ABB9AD85DE7958029A56ADB0FA291C3D91F8D22EFCBF65`
- Roughness SHA-256: `B61317E11B37CE2D932D3693FFA668077F38EE624BD1C312D4E434C918AC52A2`

## Unity integration

- Visual source: `Assets/_Project/Resources/Models/Custom/Environment/StartingIsland/Map3/Source/`
- Collision source: `Assets/_Project/Resources/Models/Custom/Environment/StartingIsland/Map3/Collision/StartingMap3_Collision.fbx`
- Material: `Assets/_Project/Art/Materials/MAT_StartingMap3.mat`
- Scene: `Assets/_Project/Scenes/Prototype.unity`
- Runtime scale: `147.6939`
- Longest horizontal dimension in the scene: approximately `280 m`
- Collision: `12,544` triangles, closed static non-convex mesh

The render model and collision model are children of one shared transform. Moving, rotating, or scaling them independently will break physical alignment.
