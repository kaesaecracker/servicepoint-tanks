import {hslToString, Theme} from '../theme.ts';
import {useEffect, useRef} from 'react';
import './PixelGridCanvas.css';

const pixelsPerRow = 352;
const pixelsPerCol = 160;
const observerMessageSize = pixelsPerCol * pixelsPerRow / 8;

enum GamePixelEntityType {
    Wall = 0x0,
    Tank = 0x1,
    Bullet = 0x2
}

function getPixelDataIndexes(bitIndex: number) {
    return {
        byteIndex: Math.floor(bitIndex / 8),
        bitInByteIndex: 7 - bitIndex % 8
    };
}

function normalizeColor(context: CanvasRenderingContext2D, color: string) {
    context.fillStyle = color;
    context.fillRect(0, 0, 1, 1);
    return context.getImageData(0, 0, 1, 1).data;
}

function parseAdditionalDataNibble(nibble: number) {
    const isPlayerMask = 1;
    const entityTypeMask = 12;

    return {
        isCurrentPlayer: (nibble & isPlayerMask) != 0,
        entityType: ((nibble & entityTypeMask) >> 2) as GamePixelEntityType,
    };
}

function drawPixelsToCanvas(
    {
        context, width, height, pixels, additional, foreground, background, playerColor, otherTanksColor
    }: {
        context: CanvasRenderingContext2D,
        width: number,
        height: number,
        pixels: Uint8ClampedArray,
        additional: Uint8ClampedArray | null,
        background: Uint8ClampedArray,
        foreground: Uint8ClampedArray,
        playerColor: Uint8ClampedArray,
        otherTanksColor: Uint8ClampedArray
    }
) {
    let additionalDataIndex = 0;
    let additionalDataByte: number | null = null;
    const nextPixelColor = (isOn: boolean) => {
        if (!isOn)
            return background;
        if (!additional)
            return foreground;

        let info;
        if (additionalDataByte === null) {
            additionalDataByte = additional[additionalDataIndex];
            additionalDataIndex++;
            info = parseAdditionalDataNibble(additionalDataByte);
        } else {
            info = parseAdditionalDataNibble(additionalDataByte >> 4);
            additionalDataByte = null;
        }

        if (info.isCurrentPlayer)
            return playerColor;

        if (info.entityType == GamePixelEntityType.Tank)
            return otherTanksColor;

        return foreground;
    };

    const imageData = context.getImageData(0, 0, width, height, {colorSpace: 'srgb'});
    const data = imageData.data;

    for (let y = 0; y < height; y++) {
        for (let x = 0; x < width; x++) {
            const pixelIndex = y * pixelsPerRow + x;
            const {byteIndex, bitInByteIndex} = getPixelDataIndexes(pixelIndex);
            const isOn = (pixels[byteIndex] & (1 << bitInByteIndex)) !== 0;
            const color = nextPixelColor(isOn);

            for (let colorChannel of [0, 1, 2, 3])
                data[pixelIndex * 4 + colorChannel] = color[colorChannel];
        }
    }

    context.putImageData(imageData, 0, 0);
}

export default function PixelGridCanvas({pixels, theme}: {
    readonly pixels: Uint8ClampedArray;
    readonly theme: Theme;
}) {
    const canvasRef = useRef<HTMLCanvasElement>(null);

    useEffect(() => {
        let ignore = false;
        const start = async () => {
            const canvas = canvasRef.current;
            if (canvas === null)
                throw new Error('canvas null');

            const drawContext = canvas.getContext('2d');
            if (!drawContext)
                throw new Error('could not get draw context');

            let additionalData: Uint8ClampedArray | null = null;
            if (pixels.length > observerMessageSize) {
                additionalData = pixels.slice(observerMessageSize);
                pixels = pixels.slice(0, observerMessageSize);
            }

            if (ignore)
                return;

            drawPixelsToCanvas({
                context: drawContext,
                width: canvas.width,
                height: canvas.height,
                pixels,
                additional: additionalData,
                background: normalizeColor(drawContext, hslToString(theme.background)),
                foreground: normalizeColor(drawContext, hslToString(theme.primary)),
                playerColor: normalizeColor(drawContext, hslToString(theme.secondary)),
                otherTanksColor: normalizeColor(drawContext, hslToString(theme.tertiary))
            });
        };

        start();
        return () => {
            ignore = true;
        };
    }, [pixels, canvasRef.current, theme]);

    return <canvas
        ref={canvasRef}
        className="PixelGridCanvas"
        width={pixelsPerRow}
        height={pixelsPerCol}
    />;
}