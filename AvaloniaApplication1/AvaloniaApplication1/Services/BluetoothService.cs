using System;
using System.Threading.Tasks;

namespace AvaloniaApplication1.Services;

public sealed class BluetoothService
{
    private BluetoothService()
    {
    }

    public static BluetoothService Instance { get; } = new();

    public async Task Scan()
    {
        Console.WriteLine("scanning");
    }

}