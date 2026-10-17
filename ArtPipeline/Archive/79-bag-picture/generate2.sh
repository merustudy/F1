#!/bin/zsh
# Round 79 third ask: look 2 redrawn so that the compartments line up with the board's squares (look2b.csv). 3 calls; the round's cap is 10.
set -e
cd "$(dirname "$0")/../../.."
for key in leather_pouch belt_pouch leather_pack; do
  [[ -f ArtPipeline/output/bag/${key}_look2b.png ]] || .venv/bin/python ArtPipeline/tools/gen_image.py --type bag --key $key --name ${key}_look2b --roster ArtPipeline/Archive/79-bag-picture/look2b.csv
done
echo done
