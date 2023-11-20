#!/bin/bash


num_cpu=$(nproc)
cpu_clock_speed=$(cat /proc/cpuinfo | grep MHz | head -1 | awk '{print $4}')
machine=$(cat /sys/class/dmi/id/product_name)
grep -q "$machine" DATA/hypervisors.txt && machine+=" (Virtual Machine)" 
total_memory=$(free -h | grep -E '^Mem:' | awk '{print $2}')
physical_nic=$(ls -l /sys/class/net/ | grep -v virtual | awk '{print $10}' | sed '1d' | tr '\n' ' ') 

echo "num_cpu         = ${num_cpu}"
echo "cpu_clock_speed = ${cpu_clock_speed} MHz"
echo "machine         = ${machine}"
echo "total_memory    = ${total_memory}"
echo "physical_nic    = ${physical_nic}"
