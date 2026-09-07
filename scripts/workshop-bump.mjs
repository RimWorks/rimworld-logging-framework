#!/usr/bin/env node
import { bumpWorkshop } from '@rimworks/mod-ci';

const stagePath = await bumpWorkshop({
  workshopId: '3733484696',
  solution: 'RimWorks.RimLogging.slnx',
});

console.log(`pushed from ${stagePath}`);
