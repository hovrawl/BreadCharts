import { dotnet } from './_framework/dotnet.js'

const is_browser = typeof window != "undefined";
if (!is_browser) throw new Error(`Expected to be running in a browser`);

const dotnetRuntime = await dotnet
    .withDiagnosticTracing(false)
    .withApplicationArgumentsFromQuery()
    .create();

const config = dotnetRuntime.getConfig();

console.log("Loading exports for assembly:", "BreadCharts.Avalonia");
const runtimeExports = await dotnetRuntime.getAssemblyExports("BreadCharts.Avalonia");
console.log("Runtime exports:", runtimeExports);
export const authService = runtimeExports.BreadCharts.Avalonia.Services.AuthService;

// Setup global auth completion listeners
if (is_browser && authService) {
    console.log("Setting up global auth listeners");
    
    // 1. postMessage listener
    window.addEventListener('message', (event) => {
        if (event.origin !== window.location.origin) return;
        
        const data = event.data;
        if (data && data.type === "spotify-auth-callback") {
            console.log("Auth callback detected via postMessage, notifying .NET");
            authService.OnAuthCompleted(data.url);
            
            if (event.source && event.source.close) {
                event.source.close();
            }
        }
    });

    // 2. BroadcastChannel listener
    try {
        const channel = new BroadcastChannel("spotify-auth");
        channel.onmessage = (event) => {
            console.log("Received message via BroadcastChannel:", event.data);
            if (event.data && event.data.type === "spotify-auth-callback") {
                console.log("Auth callback detected via BroadcastChannel, notifying .NET");
                authService.OnAuthCompleted(event.data.url);
            }
        };
    } catch (e) {
        console.warn("BroadcastChannel not supported:", e);
    }

    // 3. localStorage listener
    window.addEventListener('storage', (event) => {
        if (event.key === "spotify-auth-callback" && event.newValue) {
            console.log("Auth callback detected via localStorage, notifying .NET");
            authService.OnAuthCompleted(event.newValue);
            localStorage.removeItem("spotify-auth-callback");
        }
    });
}

if (authService && authService.Log) {
    authService.Log("AuthService exports loaded successfully");
} else {
    console.error("Failed to load AuthService exports", runtimeExports);
}

await dotnetRuntime.runMain(config.mainAssemblyName, [globalThis.location.href]);

export function openUrl(url) {
    window.location.href = url;
}

export function openPopup(url) {
    console.log("Opening auth popup with URL:", url);
    const width = 500;
    const height = 700;
    const left = (window.screen.width / 2) - (width / 2);
    const top = (window.screen.height / 2) - (height / 2);
    
    const popup = window.open(url, 'Spotify Login', `width=${width},height=${height},top=${top},left=${left}`);
    
    if (window.focus && popup) {
        popup.focus();
    } else if (!popup) {
        console.error("Popup was blocked by browser");
        authService.Log("Popup was blocked by browser. Please allow popups for this site.");
        return;
    }
}
