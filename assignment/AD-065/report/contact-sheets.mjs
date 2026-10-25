import { createCanvas, loadImage } from '@napi-rs/canvas';
import { readdirSync, writeFileSync } from 'node:fs';

const directory = '../../../tmp/pdfs/pages';
const files = readdirSync(directory).filter(file => file.endsWith('.png')).sort();
for (let start = 0; start < files.length; start += 6) {
  const group = files.slice(start, start + 6);
  const canvas = createCanvas(940, 1940);
  const context = canvas.getContext('2d');
  context.fillStyle = '#d8e0e5'; context.fillRect(0, 0, canvas.width, canvas.height);
  for (let index = 0; index < group.length; index++) {
    const image = await loadImage(`${directory}/${group[index]}`);
    const column = index % 2;
    const row = Math.floor(index / 2);
    context.drawImage(image, column * 470 + 20, row * 640 + 26, 430, 430 * image.height / image.width);
    context.fillStyle = '#14384b'; context.font = '18px Arial';
    context.fillText(group[index], column * 470 + 20, row * 640 + 22);
  }
  writeFileSync(`../../../tmp/pdfs/contact-${Math.floor(start / 6) + 1}.png`, canvas.toBuffer('image/png'));
}
