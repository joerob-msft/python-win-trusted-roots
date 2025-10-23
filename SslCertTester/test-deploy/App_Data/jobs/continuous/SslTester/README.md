# SSL Certificate Tester WebJob

This WebJob contains Python scripts for testing SSL certificate validation behavior on Windows.

## Scripts

### ssl_test.py
Uses Python's `ssl` library (OpenSSL-based). This does NOT trigger automatic certificate installation from the Windows Trusted Root Program.

### requests_test.py
Uses Python's `requests` library with `wincertstore`. On Windows, this WILL trigger automatic certificate installation from the Windows Trusted Root Program.

## Installation

Install dependencies:
```bash
pip install -r requirements.txt
```

## Usage

Test with OpenSSL (no auto-install):
```bash
python ssl_test.py www.ssl.com
```

Test with requests/WinCertStore (triggers auto-install):
```bash
python requests_test.py www.ssl.com
```
