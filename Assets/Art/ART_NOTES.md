# Arte do personagem e piso

- Original do personagem preservado: `Player/ChatGPT Image 15 de set. de 2026, 21_49_35.png`.
- `Tools/prepare_player_sprites.py` prepara os quadros individuais com transparência, alinhados em telas de 256 × 160 pixels. Requer Pillow.
- A caminhada lateral usa o mesmo ciclo espelhado para a esquerda, mantendo as passadas consistentes.
- `HouseWoodFloor.png` foi criado com a ferramenta integrada de imagens, sem CLI/API externa.

Prompt do piso:

> Create a seamless tileable texture for a 2D top-down pixel-art game: the wooden floor inside a modest cozy house. Orthographic straight overhead view, flat horizontal staggered oak planks, muted warm medium brown, subtle wood grain, small dark seams, restrained contrast so a blue-clothed black-haired player stands out. Fill the entire square image with floor. No walls, objects, furniture, rugs, characters, text, perspective or lighting gradients. Seamless edges in both directions. Crisp pixel art, square 1024x1024 PNG.

Após a importação dos assets, `RiftbornVisualSetupEditor` aplica uma vez as referências à SampleScene aberta, com suporte a Undo. Salve a cena após conferir. Para reaplicar manualmente: **Tools > Riftborn > Apply Character Visuals and House Floor**.

Controles: WASD ou setas. O disparo automático continua procurando inimigos no alcance; cada projétil emitido aciona a animação de tiro na direção do alvo por 0,18 segundos. Depois disso, o personagem volta à caminhada ou à pose parada. `Frames Per Second` controla a velocidade de caminhada.
