using SlimDX;
using SlimDX.D3DCompiler;
using SlimDX.Direct3D11;
using SlimDX.DXGI;
using SlimDX.Windows;
using Device = SlimDX.Direct3D11.Device;
using Resource = SlimDX.Direct3D11.Resource;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Input;
using System.Windows.Controls;
using System.Windows.Forms.Integration;
using System.Threading.Tasks;
using System.Threading;

namespace SimpleTriangle
{
    static class Program
    {
        static Device device;
        static SwapChain swapChain;
        static ShaderSignature inputSignature;
        static VertexShader vertexShader;
        static PixelShader pixelShader;

        static DeviceContext context;
        static RenderTargetView renderTarget;
        static DataStream vertices;
        static InputLayout layout;
        static Buffer vertexBuffer;

        static Window window;

        static WindowsFormsHost form;
        static System.Windows.Forms.Control control;

        static ManualResetEventSlim exitEvent = new ManualResetEventSlim(false);


        [System.STAThread]
        static void Main()
        {
            form = new WindowsFormsHost();
            control = new System.Windows.Forms.Control()
            {
                Width = 400,
                Height = 300,
            };
            Initialize(control);
            form.Child = control;

            window = new Window();
            window.Content = form;
            Task.Factory.StartNew(() =>
            {
                Draw();
            });

            window.ShowDialog();

            Finalize();
        }

        static void Initialize(System.Windows.Forms.Control sender)
        {
            var handle = sender.Handle;
            var description = new SwapChainDescription()
            {
                BufferCount = 2,
                Usage = Usage.RenderTargetOutput,
                OutputHandle = handle,
                IsWindowed = true,
                ModeDescription = new ModeDescription(0, 0, new Rational(60, 1), Format.R8G8B8A8_UNorm),
                SampleDescription = new SampleDescription(1, 0),
                Flags = SwapChainFlags.AllowModeSwitch,
                SwapEffect = SwapEffect.Discard
            };

            Device.CreateWithSwapChain(DriverType.Hardware, DeviceCreationFlags.Debug, description, out device, out swapChain);

            // create a view of our render target, which is the backbuffer of the swap chain we just created
            using (var resource = Resource.FromSwapChain<Texture2D>(swapChain, 0))
                renderTarget = new RenderTargetView(device, resource);

            // setting a viewport is required if you want to actually see anything
            context = device.ImmediateContext;
            var viewport = new Viewport(0.0f, 0.0f, (float)sender.Width, (float)sender.Height);
            context.OutputMerger.SetTargets(renderTarget);
            context.Rasterizer.SetViewports(viewport);

            // load and compile the vertex shader
            using (var bytecode = ShaderBytecode.CompileFromFile("triangle.fx", "VShader", "vs_4_0", ShaderFlags.None, EffectFlags.None))
            {
                inputSignature = ShaderSignature.GetInputSignature(bytecode);
                vertexShader = new VertexShader(device, bytecode);
            }

            // load and compile the pixel shader
            using (var bytecode = ShaderBytecode.CompileFromFile("triangle.fx", "PShader", "ps_4_0", ShaderFlags.None, EffectFlags.None))
                pixelShader = new PixelShader(device, bytecode);

            // create test vertex data, making sure to rewind the stream afterward
            vertices = new DataStream(12 * 3, true, true);
            vertices.Write(new Vector3(0.0f, 0.5f, 0.5f));
            vertices.Write(new Vector3(0.5f, -0.5f, 0.5f));
            vertices.Write(new Vector3(-0.5f, -0.5f, 0.5f));
            vertices.Position = 0;

            // create the vertex layout and buffer
            var elements = new[] { new InputElement("POSITION", 0, Format.R32G32B32_Float, 0) };
            layout = new InputLayout(device, inputSignature, elements);
            vertexBuffer = new Buffer(device, vertices, 12 * 3, ResourceUsage.Default, BindFlags.VertexBuffer, CpuAccessFlags.None, ResourceOptionFlags.None, 0);

            // configure the Input Assembler portion of the pipeline with the vertex data
            context.InputAssembler.InputLayout = layout;
            context.InputAssembler.PrimitiveTopology = PrimitiveTopology.TriangleList;
            context.InputAssembler.SetVertexBuffers(0, new VertexBufferBinding(vertexBuffer, 12, 0));

            // set the shaders
            context.VertexShader.Set(vertexShader);
            context.PixelShader.Set(pixelShader);

            // prevent DXGI handling of alt+enter, which doesn't work properly with Winforms
            using (var factory = swapChain.GetParent<Factory>())
                factory.SetWindowAssociation(handle, WindowAssociationFlags.IgnoreAltEnter);

            //// handle alt+enter ourselves
            //sender.KeyDown += (o, e1) =>
            //{
            //    if (e1.Alt && e1.KeyCode == System.Windows.Forms.Keys.Enter)
            //        swapChain.IsFullScreen = !swapChain.IsFullScreen;
            //};

            //// handle form size changes
            //form.UserResized += (o, e) =>
            //{
            //    renderTarget.Dispose();

            //    swapChain.ResizeBuffers(2, 0, 0, Format.R8G8B8A8_UNorm, SwapChainFlags.AllowModeSwitch);
            //    using (var resource = Resource.FromSwapChain<Texture2D>(swapChain, 0))
            //        renderTarget = new RenderTargetView(device, resource);

            //    context.OutputMerger.SetTargets(renderTarget);
            //};

        }


        static void Draw()
        {
            while (true)
            {
                if (exitEvent.IsSet) break;
                // clear the render target to a soothing blue
                context.ClearRenderTargetView(renderTarget, new Color4(0.5f, 0.5f, 1.0f));

                // draw the triangle
                context.Draw(3, 0);
                swapChain.Present(0, PresentFlags.None);

                System.Threading.Thread.Sleep(16);
            }
        }


        static void Finalize()
        {
            exitEvent.Set();

            // clean up all resources
            // anything we missed will show up in the debug output
            vertices.Close();
            vertexBuffer.Dispose();
            layout.Dispose();
            inputSignature.Dispose();
            vertexShader.Dispose();
            pixelShader.Dispose();
            renderTarget.Dispose();
            swapChain.Dispose();
            device.Dispose();
        }

    }
}
