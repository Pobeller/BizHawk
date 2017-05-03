﻿using System;
using System.Collections.Generic;

using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Nintendo.GBA
{
	public partial class MGBAHawk
	{
		private MemoryDomainList _memoryDomains;

		private MemoryDomainIntPtr _iwram;
		private MemoryDomainIntPtr _ewram;
		private MemoryDomainIntPtr _bios;
		private MemoryDomainIntPtr _palram;
		private MemoryDomainIntPtr _vram;
		private MemoryDomainIntPtr _oam;
		private MemoryDomainIntPtr _rom;
		private MemoryDomainIntPtr _sram;
		private MemoryDomainDelegate _cwram;

		private void CreateMemoryDomains(int romsize)
		{
			var le = MemoryDomain.Endian.Little;

			var mm = new List<MemoryDomain>();
			mm.Add(_iwram = new MemoryDomainIntPtr("IWRAM", le, IntPtr.Zero, 32 * 1024, true, 4));
			mm.Add(_ewram = new MemoryDomainIntPtr("EWRAM", le, IntPtr.Zero, 256 * 1024, true, 4));
			mm.Add(_bios = new MemoryDomainIntPtr("BIOS", le, IntPtr.Zero, 16 * 1024, false, 4));
			mm.Add(_palram = new MemoryDomainIntPtr("PALRAM", le, IntPtr.Zero, 1024, true, 4));
			mm.Add(_vram = new MemoryDomainIntPtr("VRAM", le, IntPtr.Zero, 96 * 1024, true, 4));
			mm.Add(_oam = new MemoryDomainIntPtr("OAM", le, IntPtr.Zero, 1024, true, 4));
			mm.Add(_rom = new MemoryDomainIntPtr("ROM", le, IntPtr.Zero, romsize, false, 4));
			mm.Add(_sram = new MemoryDomainIntPtr("SRAM", le, IntPtr.Zero, 0, true, 4)); // size will be fixed in wireup
			mm.Add(_cwram = new MemoryDomainDelegate("Combined WRAM", (256 + 32) * 1024, le, null, null, 4));

			_memoryDomains = new MemoryDomainList(mm);
			WireMemoryDomainPointers();
		}

		private void WireMemoryDomainPointers()
		{
			var s = new LibmGBA.MemoryAreas();
			LibmGBA.BizGetMemoryAreas(_core, s);

			_iwram.Data = s.iwram;
			_ewram.Data = s.wram;
			_bios.Data = s.bios;
			_palram.Data = s.palram;
			_vram.Data = s.vram;
			_oam.Data = s.oam;
			_rom.Data = s.rom;
			_sram.Data = s.sram;
			_sram.SetSize(s.sram_size);

			// special combined ram memory domain
			_cwram.Peek =
				delegate(long addr)
				{
					if (addr < 0 || addr >= (256 + 32) * 1024)
					{
						throw new IndexOutOfRangeException();
					}

					if (addr >= 256 * 1024)
					{
						return PeekWRAM(s.iwram, addr & 32767);
					}

					return PeekWRAM(s.wram, addr);
				};
			_cwram.Poke =
				delegate(long addr, byte val)
				{
					if (addr < 0 || addr >= (256 + 32) * 1024)
					{
						throw new IndexOutOfRangeException();
					}

					if (addr >= 256 * 1024)
					{
						PokeWRAM(s.iwram, addr & 32767, val);
					}
					else
					{
						PokeWRAM(s.wram, addr, val);
					}
				};

			_gpumem = new GBAGPUMemoryAreas
			{
				mmio = s.mmio,
				oam = s.oam,
				palram = s.palram,
				vram = s.vram
			};
		}

		private unsafe byte PeekWRAM(IntPtr xwram, long addr)
		{
			return ((byte*)xwram)[addr];
		}

		private unsafe void PokeWRAM(IntPtr xwram, long addr, byte value)
		{
			((byte*)xwram)[addr] = value;
		}
	}
}
