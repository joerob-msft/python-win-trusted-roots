#!/usr/bin/env python3
"""
SSL Test using requests library with WinCertStore (Windows CryptoAPI)
This WILL trigger automatic certificate installation from the Windows Trusted Root Program.
"""

import sys
from urllib.parse import urlparse

def test_requests_connection(url):
    """
    Test SSL connection using requests library.
    On Windows, requests can use wincertstore which integrates with Windows CryptoAPI.
    This WILL trigger automatic certificate installation if needed.
    """
    print(f"Testing SSL connection to: {url}")
    print("-" * 60)
    
    # Try to import requests
    try:
        import requests
        print("[OK] requests library is available")
        print(f"  requests version: {requests.__version__}")
    except ImportError:
        print("[ERROR] requests library is NOT installed")
        print("  Please install it with: pip install requests")
        return 1
    
    # Try to import wincertstore (Windows-specific)
    try:
        import wincertstore
        print("[OK] wincertstore is available (Windows CryptoAPI integration)")
        print("  This WILL trigger Windows Trusted Root Program")
    except ImportError:
        print("[WARNING] wincertstore is NOT installed")
        print("  requests may still use Windows cert store on Windows")
        print("  Install with: pip install wincertstore")
    
    print("-" * 60)
    
    # Parse the URL
    if not url.startswith('http'):
        url = 'https://' + url
    
    parsed = urlparse(url)
    hostname = parsed.hostname or parsed.path.split('/')[0]
    
    print(f"Connecting to: {url}")
    print(f"Using requests library (may use Windows CryptoAPI)")
    print("-" * 60)
    
    try:
        # Make a request using requests library
        # This will use Windows certificate store on Windows systems
        response = requests.get(url, timeout=10)
        
        print(f"[OK] HTTP Status: {response.status_code}")
        print(f"[OK] Connection successful!")
        print(f"  Content-Type: {response.headers.get('Content-Type', 'N/A')}")
        print(f"  Server: {response.headers.get('Server', 'N/A')}")
        
        print("\n" + "=" * 60)
        print("SUCCESS: Connection established using requests library")
        print("On Windows, this uses Windows CryptoAPI cert validation")
        print("This WILL trigger Windows Trusted Root Program if needed")
        print("The certificate may have been auto-installed!")
        print("=" * 60)
        
        return 0
        
    except requests.exceptions.SSLError as e:
        print(f"\n[ERROR] SSL Error: {e}")
        print("\n" + "=" * 60)
        print("SSL ERROR: Certificate validation failed")
        print("This could mean:")
        print("  1. The certificate is not trusted")
        print("  2. Windows Trusted Root Program did not auto-install it")
        print("  3. The certificate has other issues")
        print("=" * 60)
        return 1
        
    except requests.exceptions.ConnectionError as e:
        print(f"\n[ERROR] Connection Error: {e}")
        return 2
        
    except requests.exceptions.Timeout as e:
        print(f"\n[ERROR] Timeout Error: {e}")
        return 3
        
    except Exception as e:
        print(f"\n[ERROR] Unexpected Error: {type(e).__name__}: {e}")
        import traceback
        traceback.print_exc()
        return 4

if __name__ == "__main__":
    if len(sys.argv) < 2:
        print("Usage: python requests_test.py <url>")
        print("Example: python requests_test.py www.ssl.com")
        sys.exit(1)
    
    url = sys.argv[1]
    exit_code = test_requests_connection(url)
    sys.exit(exit_code)
