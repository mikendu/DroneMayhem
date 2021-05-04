# !/usr/bin/env python3
import cflinkcpp
import os

if __name__ == "__main__":
    # scan for all available Crazyflies

    os.environ["USE_CFLINK"] = "cpp"
    cfs = cflinkcpp.Connection.scan()
    print(cfs)