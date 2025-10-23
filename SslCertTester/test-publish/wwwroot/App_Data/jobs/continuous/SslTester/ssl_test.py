#!/usr/bin/env python3
"""
SSL Test using Python's ssl library (OpenSSL-based)
This should NOT trigger automatic certificate installation from the Windows Trusted Root Program.
"""

import sys
import ssl
import socket
from urllib.parse import urlparse

def test_ssl_connection(url):
    """
    Test SSL connection using Python's ssl library directly.
    This uses OpenSSL underneath and does not interact with Windows CryptoAPI.
    """
    print(f"Testing SSL connection to: {url}")
    print(f"Python SSL library version: {ssl.OPENSSL_VERSION}")
    print(f"SSL version: {ssl.OPENSSL_VERSION_INFO}")
    print("-" * 60)
    
    # Parse the URL to extract hostname and port
    if not url.startswith('http'):
        url = 'https://' + url
    
    parsed = urlparse(url)
    hostname = parsed.hostname or parsed.path.split('/')[0]
    port = parsed.port or 443
    
    print(f"Connecting to: {hostname}:{port}")
    print(f"Using ssl library (OpenSSL-based)")
    print("-" * 60)
    
    # Create a default SSL context
    # This will use OpenSSL's certificate verification, not Windows
    context = ssl.create_default_context()
    
    # For debugging, let's also try with a less strict context
    # context.check_hostname = False
    # context.verify_mode = ssl.CERT_NONE
    
    try:
        # Create a socket connection
        with socket.create_connection((hostname, port), timeout=10) as sock:
            # Wrap the socket with SSL
            with context.wrap_socket(sock, server_hostname=hostname) as ssock:
                print("[OK] SSL handshake successful!")
                print(f"Protocol version: {ssock.version()}")
                print(f"Cipher: {ssock.cipher()}")
                
                # Get certificate information
                cert = ssock.getpeercert()
                print("\nCertificate Information:")
                print(f"  Subject: {dict(x[0] for x in cert['subject'])}")
                print(f"  Issuer: {dict(x[0] for x in cert['issuer'])}")
                print(f"  Version: {cert.get('version', 'N/A')}")
                print(f"  Serial Number: {cert.get('serialNumber', 'N/A')}")
                print(f"  Not Before: {cert.get('notBefore', 'N/A')}")
                print(f"  Not After: {cert.get('notAfter', 'N/A')}")
                
                print("\n" + "=" * 60)
                print("SUCCESS: Connection established using OpenSSL")
                print("This connection uses OpenSSL cert validation,")
                print("NOT Windows CryptoAPI (no cert installation triggered)")
                print("=" * 60)
                
                return 0
                
    except ssl.SSLCertVerificationError as e:
        print(f"\n[ERROR] SSL Certificate Verification Error: {e}")
        print(f"  Error code: {e.errno}")
        print(f"  Reason: {e.reason if hasattr(e, 'reason') else 'Unknown'}")
        print("\n" + "=" * 60)
        print("EXPECTED FAILURE: Certificate validation failed")
        print("This is expected if the root CA is not in OpenSSL's cert store")
        print("OpenSSL does NOT trigger Windows Trusted Root Program")
        print("=" * 60)
        return 1
        
    except socket.gaierror as e:
        print(f"\n[ERROR] DNS Resolution Error: {e}")
        print(f"Could not resolve hostname: {hostname}")
        return 2
        
    except socket.timeout as e:
        print(f"\n[ERROR] Connection Timeout: {e}")
        return 3
        
    except Exception as e:
        print(f"\n[ERROR] Unexpected Error: {type(e).__name__}: {e}")
        import traceback
        traceback.print_exc()
        return 4

if __name__ == "__main__":
    if len(sys.argv) < 2:
        print("Usage: python ssl_test.py <url>")
        print("Example: python ssl_test.py www.ssl.com")
        sys.exit(1)
    
    url = sys.argv[1]
    exit_code = test_ssl_connection(url)
    sys.exit(exit_code)
