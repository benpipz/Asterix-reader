#!/usr/bin/env python3
"""
UDP Test Sender - Simulates UDP socket and sends data to Asterix Reader app
Usage: python test_udp_sender.py [options]
"""

import socket
import json
import time
import argparse
import sys
from datetime import datetime

def send_udp_data(host='localhost', port=5000, interval=1, count=None, json_data=None):
    """
    Send UDP packets to the specified host and port
    
    Args:
        host: Target host (default: localhost)
        port: Target port (default: 5000)
        interval: Seconds between packets (default: 1)
        count: Number of packets to send (None = infinite)
        json_data: Custom JSON data to send (None = auto-generated)
    """
    try:
        # Create UDP socket
        sock = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
        
        print(f"UDP Sender started")
        print(f"Target: {host}:{port}")
        print(f"Interval: {interval} seconds")
        print(f"Count: {'infinite' if count is None else count}")
        print("-" * 50)
        
        packet_num = 0
        
        try:
            while count is None or packet_num < count:
                # Generate test data
                if json_data:
                    data = json_data
                    # Ensure metadata field exists if not present
                    if "metadata" not in data:
                        data["metadata"] = f"Custom message #{packet_num + 1}"
                else:
                    data = {
                        "packet_number": packet_num + 1,
                        "timestamp": datetime.utcnow().isoformat(),
                        "message": f"Test UDP packet #{packet_num + 1}",
                        "metadata": f"UDP test packet #{packet_num + 1} - Contains test data with nested structure",
                        "data": {
                            "value1": packet_num * 10,
                            "value2": f"test_{packet_num}",
                            "nested": {
                                "level": 1,
                                "items": [1, 2, 3, packet_num]
                            }
                        }
                    }
                
                # Convert to JSON string and then to bytes
                json_string = json.dumps(data, indent=None)
                message_bytes = json_string.encode('utf-8')
                
                # Send packet
                sock.sendto(message_bytes, (host, port))
                
                packet_num += 1
                print(f"[{packet_num}] Sent: {len(message_bytes)} bytes - {json_string[:60]}...")
                
                # Wait before next packet
                if count is None or packet_num < count:
                    time.sleep(interval)
                    
        except KeyboardInterrupt:
            print("\n\nStopped by user (Ctrl+C)")
        finally:
            sock.close()
            print(f"\nTotal packets sent: {packet_num}")
            print("UDP Sender stopped")
            
    except socket.error as e:
        print(f"Socket error: {e}", file=sys.stderr)
        sys.exit(1)
    except Exception as e:
        print(f"Error: {e}", file=sys.stderr)
        sys.exit(1)

def main():
    parser = argparse.ArgumentParser(
        description='UDP Test Sender for Asterix Reader',
        formatter_class=argparse.RawDescriptionHelpFormatter,
        epilog="""
Examples:
  # Send 10 packets with 1 second interval
  python test_udp_sender.py --count 10
  
  # Send infinite packets with 0.5 second interval
  python test_udp_sender.py --interval 0.5
  
  # Send custom JSON data
  python test_udp_sender.py --json '{"test": "data", "value": 123}'
  
  # Send to different host/port
  python test_udp_sender.py --host 192.168.1.100 --port 5000
        """
    )
    
    parser.add_argument('--host', default='localhost',
                       help='Target host (default: localhost)')
    parser.add_argument('--port', type=int, default=5000,
                       help='Target port (default: 5000)')
    parser.add_argument('--interval', type=float, default=1.0,
                       help='Seconds between packets (default: 1.0)')
    parser.add_argument('--count', type=int, default=None,
                       help='Number of packets to send (default: infinite)')
    parser.add_argument('--json', type=str, default=None,
                       help='Custom JSON data to send (as string)')
    
    args = parser.parse_args()
    
    # Parse custom JSON if provided
    json_data = None
    if args.json:
        try:
            json_data = json.loads(args.json)
        except json.JSONDecodeError as e:
            print(f"Error: Invalid JSON format: {e}", file=sys.stderr)
            sys.exit(1)
    
    send_udp_data(
        host=args.host,
        port=args.port,
        interval=args.interval,
        count=args.count,
        json_data=json_data
    )

if __name__ == '__main__':
    main()

