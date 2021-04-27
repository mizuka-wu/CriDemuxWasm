# Demux usm file online

This Project use CriDemuxerCore and blazor to demux usm file online

## how to use

### Build wasm local

1. clone this project
2. install dotnet
3. run build.sh
4. the file is in bin/Release/net5.0/wwwroot/_framework

### Use cdn

waiting

## How to demux the usm file

### In your html file

add this to your head

```html
<script src="${pathToDir}/_framework/blazor.webassembly.js"></script>
```

### In your js file

add this function

```javascript
        /**
         * @returns {Promise<string[]>} result
         */ 
        function getDemuxFiles(file) {
            if (file) {
                const filePath = `./${file.name}`
                const fileRreader = new FileReader()
                return new Promise(resolve => {
                    fileRreader.onload = () => {
                        window.FS.writeFile(filePath, new Uint8Array(fileRreader.result))
                        resolve(DotNet.invokeMethodAsync("CriDemuxer", "Demux", filePath))
                    }
                    fileRreader.readAsArrayBuffer(file)
                })
            } else {
                throw new Error("no file")
            }
        }
```

Call this function will return the demuxed files(video: m2v, audio: adx/aix/ac3) from your uploaded file

and you can read them by use ```window.FS.readFile(filePath) => UINT8ARRAY```

## Sample

Please read the `wwwroot/index.html` and run `start.sh`
