using Resonance.RPC;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Resonance.Examples.RPC.Common
{
    public interface IRemoteDrawingBoardService
    {
        event EventHandler<RemoteRectAddedEventArgs> RectangleAdded;

        ObservableCollection<RemoteRect> Rectangles { get; set; }

        void StartRectangle(RemotePoint position);

        void SizeRectangle(RemoteRect size);

        void FinishRectangle(RemoteRect rect);

        Task<String> GetWelcomeMessage(String str, int a);

        int GetRectanglesCount();

        Task<int> GetRectanglesCountAsync();

        Task<int> CalcAsync(int a, int b);
    }
}
