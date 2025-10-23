# Python SSL Code Comparison: OpenSSL vs Windows CryptoAPI

This document highlights the key differences between Python code that uses OpenSSL (does NOT trigger Windows Trusted Root Program) versus code that uses Windows CryptoAPI (WILL trigger automatic certificate installation).

## 🔴 Approach 1: OpenSSL (No Automatic Certificate Installation)

### Code Example: Using Python's `ssl` Library

```python
import ssl
import socket

def test_with_openssl(hostname, port=443):
    """
    Uses OpenSSL for certificate validation.
    Does NOT trigger Windows Trusted Root Program.
    """
    # Create a default SSL context (uses OpenSSL)
    context = ssl.create_default_context()
    
    try:
        # Create socket and wrap with SSL
        with socket.create_connection((hostname, port), timeout=10) as sock:
            with context.wrap_socket(sock, server_hostname=hostname) as ssock:
                print(f"Connected using OpenSSL")
                print(f"Protocol: {ssock.version()}")
                
                # Get certificate info
                cert = ssock.getpeercert()
                print(f"Subject: {dict(x[0] for x in cert['subject'])}")
                
                return True
                
    except ssl.SSLCertVerificationError as e:
        print(f"Certificate verification failed: {e}")
        # Certificate is NOT automatically installed
        return False
```

### Key Characteristics:
- ✅ Uses Python's built-in `ssl` module
- ✅ Validates against OpenSSL's certificate store
- ✅ Does **NOT** interact with Windows certificate store
- ✅ Does **NOT** trigger Windows Trusted Root Program
- ❌ Will fail if root CA is not in OpenSSL's store
- ❌ Certificate must be manually installed if needed

### When to Use:
- Cross-platform applications (Linux, macOS, Windows)
- When you want explicit control over certificate validation
- When you don't want automatic certificate installation
- Testing/development scenarios where you need predictable behavior

---

## 🟢 Approach 2: Windows CryptoAPI (Automatic Certificate Installation)

### Code Example: Using `requests` with `wincertstore`

```python
import requests

def test_with_cryptoapi(url):
    """
    Uses Windows CryptoAPI for certificate validation.
    WILL trigger Windows Trusted Root Program on first connection.
    """
    # On Windows, requests automatically uses the system certificate store
    # when wincertstore is installed
    
    try:
        response = requests.get(url, timeout=10)
        
        print(f"Connected using Windows CryptoAPI")
        print(f"Status: {response.status_code}")
        print(f"Server: {response.headers.get('Server', 'N/A')}")
        
        # If this succeeds, the certificate was validated
        # If the root CA wasn't trusted, Windows may have auto-installed it
        return True
        
    except requests.exceptions.SSLError as e:
        print(f"SSL Error: {e}")
        # Certificate validation failed
        # Auto-install may not have occurred or cert has other issues
        return False
```

### Key Characteristics:
- ✅ Uses `requests` library with Windows integration
- ✅ Validates against Windows certificate store
- ✅ **WILL** trigger Windows Trusted Root Program
- ✅ Automatically installs missing trusted root certificates
- ✅ Seamless user experience (certificates just work)
- ⚠️ Windows-specific behavior

### When to Use:
- Windows-only applications
- Production applications where you want seamless certificate handling
- When you trust Windows Trusted Root Program
- Web scraping, API clients, automated tools

---

## 📊 Side-by-Side Comparison

| Feature | OpenSSL (`ssl` module) | Windows CryptoAPI (`requests`) |
|---------|------------------------|--------------------------------|
| **Library** | `ssl` + `socket` | `requests` + `wincertstore` |
| **Certificate Store** | OpenSSL's store | Windows system store |
| **Auto-Install Certs** | ❌ No | ✅ Yes (via Trusted Root Program) |
| **Platform** | Cross-platform | Windows-specific behavior |
| **Certificate Updates** | Manual | Automatic |
| **Predictability** | High | Medium (depends on Windows) |
| **Ease of Use** | Lower (more code) | Higher (simpler API) |
| **Control** | Full control | Less control |

---

## 🔧 Installation Requirements

### For OpenSSL Approach (Approach 1):
```bash
# No additional packages needed - built into Python
# Just use the standard library
```

### For Windows CryptoAPI Approach (Approach 2):
```bash
# Install requests and wincertstore
pip install requests wincertstore
```

---

## 💡 Complete Working Examples

### Example 1: OpenSSL Test (ssl_test.py)

```python
#!/usr/bin/env python3
"""Test SSL connection using OpenSSL (no auto-install)"""

import sys
import ssl
import socket

def test_ssl_connection(hostname, port=443):
    print(f"Testing connection to: {hostname}:{port}")
    print(f"Using: Python ssl library (OpenSSL)")
    
    # Create default SSL context
    context = ssl.create_default_context()
    
    try:
        # Connect with SSL
        with socket.create_connection((hostname, port), timeout=10) as sock:
            with context.wrap_socket(sock, server_hostname=hostname) as ssock:
                print("[OK] SSL handshake successful!")
                
                # Get certificate info
                cert = ssock.getpeercert()
                print(f"Subject: {dict(x[0] for x in cert['subject'])}")
                print(f"Issuer: {dict(x[0] for x in cert['issuer'])}")
                
                print("\nSUCCESS: OpenSSL validated the certificate")
                print("No automatic certificate installation occurred")
                return 0
                
    except ssl.SSLCertVerificationError as e:
        print(f"[ERROR] Certificate verification failed: {e}")
        print("\nEXPECTED BEHAVIOR:")
        print("- Root CA is not in OpenSSL's certificate store")
        print("- Certificate was NOT automatically installed")
        print("- This is normal for OpenSSL approach")
        return 1
        
    except Exception as e:
        print(f"[ERROR] Connection failed: {e}")
        return 2

if __name__ == "__main__":
    hostname = sys.argv[1] if len(sys.argv) > 1 else "www.ssl.com"
    sys.exit(test_ssl_connection(hostname))
```

### Example 2: Windows CryptoAPI Test (requests_test.py)

```python
#!/usr/bin/env python3
"""Test SSL connection using requests/WinCertStore (auto-install)"""

import sys

def test_requests_connection(url):
    print(f"Testing connection to: {url}")
    print(f"Using: requests library (Windows CryptoAPI)")
    
    try:
        import requests
        print("[OK] requests library is available")
    except ImportError:
        print("[ERROR] requests not installed")
        print("Install with: pip install requests wincertstore")
        return 1
    
    try:
        import wincertstore
        print("[OK] wincertstore is available")
        print("      This WILL trigger Windows Trusted Root Program")
    except ImportError:
        print("[WARNING] wincertstore not installed")
        print("          Install with: pip install wincertstore")
    
    # Ensure URL has protocol
    if not url.startswith('http'):
        url = f'https://{url}'
    
    try:
        # Make HTTPS request
        response = requests.get(url, timeout=10)
        
        print(f"[OK] HTTP Status: {response.status_code}")
        print(f"[OK] Connection successful!")
        
        print("\nSUCCESS: Windows CryptoAPI validated the certificate")
        print("If the root CA was missing, it may have been auto-installed")
        print("by the Windows Trusted Root Program!")
        return 0
        
    except requests.exceptions.SSLError as e:
        print(f"[ERROR] SSL Error: {e}")
        print("\nCertificate validation failed")
        print("Windows Trusted Root Program may not have auto-installed it")
        return 1
        
    except Exception as e:
        print(f"[ERROR] Connection failed: {e}")
        return 2

if __name__ == "__main__":
    url = sys.argv[1] if len(sys.argv) > 1 else "www.ssl.com"
    sys.exit(test_requests_connection(url))
```

---

## 🎯 Key Takeaways

### Use OpenSSL (`ssl` module) When:
1. You need **consistent behavior across platforms**
2. You want **explicit control** over certificate validation
3. You're **testing/debugging** certificate issues
4. You want to **avoid automatic certificate installation**
5. You need **predictable failure modes**

### Use Windows CryptoAPI (`requests`) When:
1. You're building **Windows-only applications**
2. You want **seamless certificate handling**
3. You trust **Windows Trusted Root Program**
4. You want **certificates to "just work"** for end users
5. You're building **production applications** on Windows

---

## 🔍 Detecting Which Method is Being Used

### Check if code uses OpenSSL:
```python
import ssl
# If code uses ssl.create_default_context() or ssl.SSLContext
# and socket connections, it's using OpenSSL
```

### Check if code uses Windows CryptoAPI:
```python
import requests
# If code uses requests.get(), requests.post(), etc.
# on Windows with wincertstore installed, it uses CryptoAPI
```

### Verify at runtime:
```python
import ssl
print(f"OpenSSL Version: {ssl.OPENSSL_VERSION}")

try:
    import wincertstore
    print("WinCertStore is available - requests will use Windows cert store")
except ImportError:
    print("WinCertStore not installed - requests may use certifi bundle")
```

---

## � Loading Custom Root Certificates with OpenSSL

One of the key advantages of the OpenSSL approach is that you have **complete control** over which root certificates to trust. You can load your own trusted root certificates from a PEM file:

### Method 1: Add Custom Roots to Default CAs

```python
import ssl
import socket

def test_with_custom_roots(hostname, pem_file):
    # Create SSL context with default CAs
    context = ssl.create_default_context()
    
    # Add your custom trusted root certificates
    # The PEM file can contain one or more CA certificates
    context.load_verify_locations(cafile=pem_file)
    
    # Optionally, load from a directory of PEM files
    # context.load_verify_locations(capath="/path/to/ca/certificates/")
    
    with socket.create_connection((hostname, 443), timeout=10) as sock:
        with context.wrap_socket(sock, server_hostname=hostname) as ssock:
            print(f"✅ Connected using custom CA bundle: {pem_file}")
            return ssock.getpeercert()

# Example usage:
cert = test_with_custom_roots("internal-server.company.local", "my-trusted-roots.pem")
```

### Method 2: Use ONLY Custom Roots (Exclude System CAs)

```python
import ssl
import socket

def test_only_custom_roots(hostname, pem_file):
    # Create a fresh context WITHOUT loading default system CAs
    context = ssl.SSLContext(ssl.PROTOCOL_TLS_CLIENT)
    
    # Enable hostname checking and certificate verification
    context.check_hostname = True
    context.verify_mode = ssl.CERT_REQUIRED
    
    # Load ONLY your custom trusted roots
    context.load_verify_locations(cafile=pem_file)
    
    # This will ONLY trust certificates signed by CAs in your PEM file
    with socket.create_connection((hostname, 443), timeout=10) as sock:
        with context.wrap_socket(sock, server_hostname=hostname) as ssock:
            print("✅ Connected using ONLY custom CA bundle!")
            return ssock.getpeercert()

# Example usage:
cert = test_only_custom_roots("internal-server.company.local", "company-root-ca.pem")
```

### Creating a PEM File

A PEM file is a text file containing one or more certificates in PEM format:

```text
-----BEGIN CERTIFICATE-----
MIIDdzCCAl+gAwIBAgIEAgAAuTANBgkqhkiG9w0BAQUFADBaMQswCQYDVQQGEwJJ
[... certificate data ...]
-----END CERTIFICATE-----
-----BEGIN CERTIFICATE-----
MIIEMjCCAxqgAwIBAgIBATANBgkqhkiG9w0BAQUFADB7MQswCQYDVQQGEwJHQjEb
[... another certificate ...]
-----END CERTIFICATE-----
```

**Important**: The PEM file must contain the **root CA certificate**, not the server certificate.

### Exporting a Root Certificate from Windows to PEM

```powershell
# PowerShell: Export a root certificate to PEM format
$cert = Get-ChildItem -Path Cert:\LocalMachine\Root | Where-Object {$_.Subject -like "*Your CA Name*"} | Select-Object -First 1
$pemBytes = @(
    '-----BEGIN CERTIFICATE-----'
    [System.Convert]::ToBase64String($cert.RawData, 'InsertLineBreaks')
    '-----END CERTIFICATE-----'
)
$pemBytes | Out-File -FilePath "my-root-ca.pem" -Encoding ASCII
```

### Benefits of Loading Custom Roots

✅ **Precise control** over which Certificate Authorities to trust  
✅ **Add private/internal CA certificates** for internal services  
✅ **Restrict trust** to specific CAs for enhanced security  
✅ **Consistent behavior** across different environments  
✅ **No reliance** on system certificate stores  
✅ **Perfect for testing** and development scenarios

---

## �📚 Additional Resources

- **OpenSSL**: https://docs.python.org/3/library/ssl.html
- **SSL Context**: https://docs.python.org/3/library/ssl.html#ssl.SSLContext
- **Requests**: https://requests.readthedocs.io/
- **WinCertStore**: https://pypi.org/project/wincertstore/
- **Windows Trusted Root Program**: https://learn.microsoft.com/security/trusted-root/program-requirements
- **Certificate Validation**: https://docs.python.org/3/library/ssl.html#certificate-validation

---

## 🧪 Testing Your Application

1. **Check existing certificate store** (before test):
   ```powershell
   Get-ChildItem Cert:\LocalMachine\Root | Where-Object {$_.Subject -like "*YourCA*"}
   ```

2. **Run OpenSSL test** (should fail if cert not installed):
   ```bash
   python ssl_test.py www.example.com
   ```

3. **Verify cert still not installed**:
   ```powershell
   Get-ChildItem Cert:\LocalMachine\Root | Where-Object {$_.Subject -like "*YourCA*"}
   ```

4. **Run requests test** (should succeed and auto-install):
   ```bash
   python requests_test.py www.example.com
   ```

5. **Verify cert was installed**:
   ```powershell
   Get-ChildItem Cert:\LocalMachine\Root | Where-Object {$_.Subject -like "*YourCA*"}
   ```

---

**Need Help?** Check the [README.md](README.md) for full application setup and deployment instructions.
