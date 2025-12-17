export interface UdpReceiverConfig {
  port: number;
  listenAddress: string;
  joinMulticastGroup: boolean;
  multicastAddress?: string;
}

export interface PcapReceiverConfig {
  filePath: string;
  filter?: string;
}

export interface ReceiverStatus {
  mode: 'UDP' | 'PCAP' | null;
  isRunning: boolean;
  config?: UdpReceiverConfig | PcapReceiverConfig;
}

