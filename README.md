# BlinkingEye

[BlinkingEye](https://github.com/wincentbalin/BlinkingEye) is a remote viewer for Windows over low-throughput TCP/IP links.

## Requirements

 * .NET 3.5
 * WPF

Optionally:

 * PngOptimizerDll.dll (you need to place the 32 bit DLL into the directory with **BlinkingEye** binary.)

## Features

 * Aimed originally at viewing remote Windows desktop in a web browser over a serial line (57600 bps)
 * Single .NET executable file with all files included
 * HTML5 client with AJAX and Canvas

## Use cases

You may run the **BlinkingEye** binary with the following command line arguments.

### Running with password

```
BlinkingEye xyz1
```

These options start **BlinkingEye** with the specified password `xyz1`. The server listens on all available IP addresses at the port 3130 and captures the screen number 1.

### Running with port and password

```
BlinkingEye 3131 xyz1
```

These options start **BlinkingEye** with the specified password `xyz1`. The server listens on all available IP addresses at the port `3131` and captures the screen number `1`.

### Running with IP address, port and password

```
BlinkingEye 192.168.2.1 3131 xyz1
```

These options start **BlinkingEye** with the specified password `xyz1`. The server listens on the IP address `192.168.2.1` at the port `3131` and captures the screen number `1`.

### Running with screen number IP address, port and password

```
BlinkingEye 2 192.168.2.1 3131 xyz1
```

These options start **BlinkingEye** with the specified password `xyz1`. The server listens on the IP address `192.168.2.1` at the port `3131` and captures the screen number `2`.

## License

**BlinkingEye** is available under the GNU Public License v2.0. See the [LICENSE](LICENSE) file for more info.

## Author

Wincent Balin

You can find me at https://wincent.balin.at/
