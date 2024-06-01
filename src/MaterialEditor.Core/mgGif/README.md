# mgGIF
> A unity library to parse a GIF file and extracts the images, just for fun

![Butterfly](https://gwaredd.github.io/mgGif/butterfly.gif)

## Installation

Copy [Assets\mgGif\mgGif.cs](https://github.com/gwaredd/mgGif/blob/master/Assets/mgGif/mgGif.cs) to your project.

Alternatively, the [upm](https://github.com/gwaredd/mgGif/tree/upm) branch can be pulled directly into the `Packages` directory, e.g.

```
git clone -b upm git@github.com:gwaredd/mgGif.git
```

## Usage

Pass a `byte[]` of the GIF file and loop through results.

```cs

byte[] data = File.ReadAllBytes( "some.gif" );

using( var decoder = new MG.GIF.Decoder( data ) )
{
    var img = decoder.NextImage();

    while( img != null )
    {
        Texture2D tex = img.CreateTexture();
        int delay = img.Delay;
        
        img = decoder.NextImage();
    }
}
```

See [AnimatedTextures.cs](https://github.com/gwaredd/mgGif/blob/main/Assets/Scenes/Scripts/AnimatedTextures.cs) for an example

**NB:** For speed the decoder will reuse buffers between each `NextImage()` call. If you need to keep the raw image data then ensure you `Clone()` it first.

For an additional performance improvement, uncomment `mgGIF_UNSAFE` at the top of the file and allow unsafe code compilation in the assembly.

## Benchmarks

Benchmarks of the time to decode three test animations using different libraries.

| Library               | Editor    | Mono      | IL2CPP    |
|-----------------------|-----------|----------|------------|
| UniGif                | 7321 ms   | 3790 ms  | 3178 ms    |
| Unity-GifDecoder      | 365  ms   | 123  ms  | 88   ms    |
| mgGif                 | 247  ms   | 112  ms  | 80   ms    |
| mgGif (Unsafe Mode)   | 280  ms   | 106  ms  | 70   ms    |


