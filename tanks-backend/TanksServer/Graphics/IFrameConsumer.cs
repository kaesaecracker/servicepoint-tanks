namespace TanksServer.Graphics;

internal interface IFrameConsumer
{
    Task OnFrameDoneAsync(GamePixelGrid gamePixelGrid, Bitmap observerPixels);
}
